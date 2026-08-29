using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    public PlayerActionMode actionMode { get; private set; } = PlayerActionMode.Waiting;

    private const PokerPosition PLAYER_POSITION = PokerPosition.Joker;

    [SerializeField] private Camera interactionCamera;
    [SerializeField] private LayerMask chipLayerMask;

    void Start()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }
        PokerGameManager.Instance.GameStateChanged += SetPlayerAction;
        PokerGameManager.Instance.PerformedPlayerAction += ActionAnimationHandler;
    }

    public void ActionAnimationHandler(object sender, PokerGameManager.PokerEvent e)
    {
        if (e.Player != PLAYER_POSITION) return;

        PokerGameManager.Instance.SetPausedForAnimationEvents(false);
    }

    void Update()
    {
        if (actionMode == PlayerActionMode.Waiting) return;

        HandleChipInteraction();
        //HandleActionKeys();
    }

    private void HandleChipInteraction()
    {
        // bool interactPressed = GameManager.Instance.Controls.Player.ChipInteract.WasPressedThisFrame();
        // if (!interactPressed) return;

        Chip chip = RaycastForChip();
        if (chip == null || chip.IsLocked) return;

        if (chip.location == ChipLocation.Stack)
        {
            if (BettingManager.Instance.BetAmount(PLAYER_POSITION, chip.chipValue))
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

    /*private void HandleActionKeys()
    {
        if (GameManager.Instance.Controls.Player.Fold.WasPressedThisFrame())
        {
            PerformFold();
            return;
        }
 
        if (GameManager.Instance.Controls.Player.CheckorCall.WasPressedThisFrame())
        {
            PerformCheckOrCall();
            return;
        }
 
        if (GameManager.Instance.Controls.Player.SubmitBet.WasPressedThisFrame())
        {
            PerformSubmitBet();
            return;
        }
    }*/

    private int AmountToCall()
    {
        int highestBet = BettingManager.Instance.GetHighestBet(PokerGameManager.Instance.ActivePlayers);
        int myBet = BettingManager.Instance.GetBet(PLAYER_POSITION);
        return Mathf.Max(0, highestBet - myBet);
    }
 
    private bool CanCheck() => AmountToCall() == 0 && actionMode != PlayerActionMode.PreflopBetting;
 
    private void PerformFold()
    {
        EventInvokingSubmitAction(PokerAction.Fold, 0);
        EndTurn();
    }
 
    private void PerformCheckOrCall()
    {
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
 
    private void PerformSubmitBet()
    {
        int builtAmount = BettingManager.Instance.PlayerBet;
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
        if (amountToCall == 0)
        {
            action = PokerAction.Bet; // nothing was owed, this opens the betting
        }
        else if (builtAmount == amountToCall)
        {
            action = PokerAction.Call; // matched exactly, no extra on top
        }
        else
        {
            action = PokerAction.Raise; // built more than what was owed
        }
 
        EventInvokingSubmitAction(action, builtAmount);
        
        EndTurn();
    }

    private void EventInvokingSubmitAction(PokerAction action, int amount)
    {
        PokerGameManager.Instance.SubmitAction(PLAYER_POSITION, action, amount);
        PokerGameManager.Instance.PerformedPlayerAction?.Invoke(this, new PokerGameManager.PokerEvent(PLAYER_POSITION, action, amount, false));
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