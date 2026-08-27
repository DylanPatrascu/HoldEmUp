using System;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public enum ActionMode
    {
        Waiting,
        Betting,
        Preflop
    }

    public ActionMode actionMode { get; private set; } = ActionMode.Waiting;

    void Start()
    {
        PokerGameManager.Instance.GameStateChanged += SetPlayerAction;
    }

    void Update()
    {
        if (actionMode == ActionMode.Preflop || actionMode == ActionMode.Betting)
        {
            Debug.Log("Listening to player input");
        }
    }

    public void SetPlayerAction(object sender, EventArgs e)
    {
        switch (PokerGameManager.Instance.CurrentGameState)
        {
            case PokerGameManager.GameState.Preflop:
                actionMode = ActionMode.Preflop;
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