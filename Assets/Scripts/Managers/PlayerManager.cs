using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public PlayerActionMode actionMode { get; private set; } = PlayerActionMode.Waiting;

    private const PokerPosition PLAYER_POSITION = PokerPosition.Joker;

    [SerializeField] private Camera interactionCamera;
    [SerializeField] private LayerMask chipLayerMask;

    [Header("Keybinds")]
    [SerializeField] private KeyCode chipInteractKey = KeyCode.E;
    [SerializeField] private KeyCode submitBetKey = KeyCode.Return;
    [SerializeField] private KeyCode checkOrCallKey = KeyCode.C;
    [SerializeField] private KeyCode foldKey = KeyCode.F;

    void Start()
    {
        PokerGameManager.Instance.GameStateChanged += SetPlayerAction;
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }
    }

    void Update()
    {
        if (actionMode == PlayerActionMode.Waiting) return;

    }

    private void HandleChipInteraction()
    {
        bool interactPressed = Input.GetKeyDown(chipInteractKey) || Input.GetMouseButtonDown(0);
        if (!interactPressed) return;

    }

    public void SetPlayerAction(object sender, EventArgs e)
    {
        if (!PokerGameManager.Instance.awaitingPlayer)
        {
            actionMode = PlayerActionMode.Waiting;
            return;
        }

        switch (PokerGameManager.Instance.CurrentGameState)
        {
            case PokerGameManager.GameState.Preflop:
                actionMode = PlayerActionMode.PreflopBetting;
                break;
            case PokerGameManager.GameState.Postflop:
                actionMode = PlayerActionMode.Betting;
                break;
            default:
                actionMode = PlayerActionMode.Waiting;
                break;
        }
    }
}