using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public enum ActionMode
    {
        Waiting,
        Betting,
        PreflopBetting
    }

    public ActionMode actionMode { get; private set; } = ActionMode.Waiting;

    void Start()
    {
        PokerGameManager.Instance.GameStateChanged += SetPlayerAction;
    }

    void Update()
    {
        if (actionMode == ActionMode.PreflopBetting)
        {
            Debug.Log("Listening to player input: no checking");
        }
        if (actionMode == ActionMode.Betting)
        {
            Debug.Log("Listening to player input: checking allowed");
        }
    }

    public void SetPlayerAction(object sender, EventArgs e)
    {
        if (!PokerGameManager.Instance.awaitingPlayer)
        {
            actionMode = ActionMode.Waiting;
            return;
        }

        switch (PokerGameManager.Instance.CurrentGameState)
        {
            case PokerGameManager.GameState.Preflop:
                actionMode = ActionMode.PreflopBetting;
                break;
            case PokerGameManager.GameState.Postflop:
                actionMode = ActionMode.Betting;
                break;
            default:
                actionMode = ActionMode.Waiting;
                break;
        }
    }
}