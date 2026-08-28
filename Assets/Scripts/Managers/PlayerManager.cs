using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public PlayerActionMode actionMode { get; private set; } = PlayerActionMode.Waiting;

    void Start()
    {
        PokerGameManager.Instance.GameStateChanged += SetPlayerAction;
    }

    void Update()
    {
        if (actionMode == PlayerActionMode.PreflopBetting)
        {
            Debug.Log("Listening to player input: no checking");
        }
        if (actionMode == PlayerActionMode.Betting)
        {
            Debug.Log("Listening to player input: checking allowed");
        }
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