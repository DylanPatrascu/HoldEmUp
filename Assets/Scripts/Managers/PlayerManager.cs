using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public PlayerActionMode actionMode { get; private set; } = PlayerActionMode.Waiting;

    private const PokerPosition PLAYER_POSITION = PokerPosition.Joker;

    [SerializeField] private Camera interactionCamera;
    [SerializeField] private LayerMask chipLayerMask;

    [SerializeField]
    private CinemachineCamera cCam;

    private InputAction chipInteractAction;
    private InputAction foldAction;
    private InputAction checkOrCallAction;
    private InputAction submitBetAction;
    private InputAction switchCameraAction;

    void Start()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }
        PokerGameManager.Instance.GameStateChanged += SetPlayerAction;
        PokerGameManager.Instance.PerformedPlayerAction += ActionAnimationHandler;
        GameManager.Instance.PauseGameUI.ResumeRequested += OnResumeRequested;
    }

    private void OnEnable()
    {
        if (GameManager.Instance == null || GameManager.Instance.PlayerInputSystem == null)
        {
            Debug.LogWarning("PlayerManager enabled before GameManager/PlayerInput was ready.");
            return;
        }

        var actions = GameManager.Instance.PlayerInputSystem.actions;

        chipInteractAction = actions.FindAction("Chip Interact");
        foldAction = actions.FindAction("Fold");
        checkOrCallAction = actions.FindAction("Check or Call");
        submitBetAction = actions.FindAction("Submit Bet");
        switchCameraAction = actions.FindAction("Switch Camera");

        if (chipInteractAction != null) chipInteractAction.performed += OnChipInteractPerformed;
        if (foldAction != null) foldAction.performed += OnFoldPerformed;
        if (checkOrCallAction != null) checkOrCallAction.performed += OnCheckOrCallPerformed;
        if (submitBetAction != null) submitBetAction.performed += OnSubmitBetPerformed;
        if (switchCameraAction != null) switchCameraAction.performed += OnSwitchCameraPerformed;
    }

    private void OnDisable()
    {
        if (chipInteractAction != null) chipInteractAction.performed -= OnChipInteractPerformed;
        if (foldAction != null) foldAction.performed -= OnFoldPerformed;
        if (checkOrCallAction != null) checkOrCallAction.performed -= OnCheckOrCallPerformed;
        if (submitBetAction != null) submitBetAction.performed -= OnSubmitBetPerformed;
        if (switchCameraAction != null) switchCameraAction.performed -= OnSwitchCameraPerformed;

        if (PokerGameManager.Instance != null)
        {
            PokerGameManager.Instance.GameStateChanged -= SetPlayerAction;
            PokerGameManager.Instance.PerformedPlayerAction -= ActionAnimationHandler;
        }

        if (GameManager.Instance != null && GameManager.Instance.PauseGameUI != null)
        {
            GameManager.Instance.PauseGameUI.ResumeRequested -= OnResumeRequested;
        }
    }

    public void ActionAnimationHandler(object sender, PokerGameManager.PokerEvent e)
    {
        Debug.Log($"{e.Player} did {e.Action} and PausedForAnimations is {PokerGameManager.Instance.PausedForAnimationEvents}");
        if (e.Player != PLAYER_POSITION) return;

        PokerGameManager.Instance.SetPausedForAnimationEvents(false);
    }

    // Fired when the pause menu's Resume button is clicked.
    public void OnResumeRequested(object sender, EventArgs e)
    {
        if (!GameManager.Instance.IsGamePaused) return;

        GameManager.Instance.ResumeGame();
        GameManager.Instance.PlayerInputSystem.SwitchCurrentActionMap("Player");
    }

    private void OnChipInteractPerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log($"Player is trying to interact with chip with action mode: {actionMode}");
        if (actionMode == PlayerActionMode.Waiting) return;

        Chip chip = RaycastForChip();
        if (chip == null || chip.IsLocked) return;

        if (chip.location == ChipLocation.Stack)
        {
            if (BettingManager.Instance.BetAmount(PLAYER_POSITION, chip.chipValue, PokerGameManager.Instance.ActivePlayers))
                BettingVisualManager.Instance.MoveChip(chip, ChipLocation.Table);
        }
        else
        {
            if (BettingManager.Instance.RemoveAmount(chip.chipValue))
                BettingVisualManager.Instance.MoveChip(chip, ChipLocation.Stack);
        }

        BettingVisualManager.Instance.UpdateBet(BettingManager.Instance.PlayerBet);
    }

    private Chip RaycastForChip()
    {
        if (interactionCamera == null) return null;

        Ray ray = new Ray(interactionCamera.transform.position, interactionCamera.transform.forward);
        RaycastHit hit;

        Debug.DrawRay(ray.origin, ray.direction * 20f, Color.red);

        if (Physics.Raycast(ray, out hit, 20f, chipLayerMask))
        {
            return hit.collider.GetComponent<Chip>();
        }

        return null;
    }

    private int AmountToCall()
    {
        int highestBet = BettingManager.Instance.GetHighestBet(PokerGameManager.Instance.ActivePlayers);
        int myBet = BettingManager.Instance.GetBet(PLAYER_POSITION);
        return Mathf.Max(0, highestBet - myBet);
    }

    private bool CanCheck() => AmountToCall() == 0 && actionMode != PlayerActionMode.PreflopBetting;

    private void OnFoldPerformed(InputAction.CallbackContext ctx)
    {
        if (actionMode == PlayerActionMode.Waiting) return;

        EventInvokingSubmitAction(PokerAction.Fold, 0);
        EndTurn();
    }

    private void OnCheckOrCallPerformed(InputAction.CallbackContext ctx)
    {
        if (actionMode == PlayerActionMode.Waiting) return;

        int amountToCall = AmountToCall();

        if (amountToCall == 0)
        {
            if (!CanCheck())
            {
                Debug.LogWarning("Checking isn't allowed during preflop betting.");
                return;
            }
            EventInvokingSubmitAction(PokerAction.Check, 0);
        }
        else
        {
            EventInvokingSubmitAction(PokerAction.Call, amountToCall);
        }

        EndTurn();
    }

    private void OnSubmitBetPerformed(InputAction.CallbackContext ctx)
    {
        if (actionMode == PlayerActionMode.Waiting) return;

        int builtAmount = BettingManager.Instance.PlayerBet;
        int currentContribution = BettingManager.Instance.GetBet(PLAYER_POSITION);
        int amountToCall = AmountToCall();

        // Nothing on the table yet - nothing to submit as a bet/raise.
        if (builtAmount <= 0)
        {
            Debug.LogWarning("Add chips to the table before submitting a bet.");
            return;
        }

        // Not enough to even match the current bet - block rather than
        // silently under-calling.
        if (builtAmount < amountToCall)
        {
            Debug.LogWarning("Not enough chips on the table to call - add more, or pull them back and fold instead.");
            return;
        }

        PokerAction action;
        int actionAmount;
        if (amountToCall == 0)
        {
            action = PokerAction.Bet; // nothing was owed, this opens the betting
            actionAmount = builtAmount;
        }
        else if (builtAmount == amountToCall)
        {
            action = PokerAction.Call; // matched exactly, no extra on top
            actionAmount = amountToCall;
        }
        else
        {
            action = PokerAction.Raise; // built more than what was owed
            actionAmount = builtAmount - currentContribution;
        }

        PokerGameManager.Instance.SetPausedForAnimationEvents(true);
        EventInvokingSubmitAction(action, actionAmount);

        EndTurn();
    }

    private void OnSwitchCameraPerformed(InputAction.CallbackContext ctx)
    {
        cCam.Priority.Value = cCam.Priority.Value == 0 ? 3 : 0;
    }

    private void EventInvokingSubmitAction(PokerAction action, int amount)
    {
        PokerGameManager.Instance.SubmitAction(PLAYER_POSITION, action, amount);
        var actionEvent = new PokerGameManager.PokerEvent(PLAYER_POSITION, action, amount, false);
        Debug.Log($"[PlayerManager] Player action: {action} amount={amount}");
        PokerGameManager.Instance.PerformedPlayerAction?.Invoke(this, actionEvent);
    }

    private void EndTurn()
    {
        PokerGameManager.Instance.SetAwaitingPlayer(false);
    }

    public void SetPlayerAction(object sender, EventArgs e)
    {
        if (!PokerGameManager.Instance.awaitingPlayer)
        {
            actionMode = PlayerActionMode.Waiting;
            BettingVisualManager.Instance.SetAllChipsLocked(true);
            return;
        }

        actionMode = PokerGameManager.Instance.CurrentGameState switch
        {
            PokerGameManager.GameState.Preflop => PlayerActionMode.PreflopBetting,
            PokerGameManager.GameState.Postflop => PlayerActionMode.Betting,
            _ => PlayerActionMode.Waiting,
        };

        Debug.Log($"Changed player action mode {actionMode}");
        BettingVisualManager.Instance.SetChipsLocked(ChipLocation.Stack, actionMode == PlayerActionMode.Waiting);
    }
}