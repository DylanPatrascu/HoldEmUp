using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokerGameManager : MonoBehaviour
{
    private enum GameState
    {
        StartGame,
        Preflop, // Betting without check
        Turn, // Betting with check
        EndGame
    }

    public static PokerGameManager Instance;

    private bool awaitingPlayer = false;
    private List<PokerPosition> ActivePlayers;
    private PokerPosition SmallBlind = PokerPosition.Joker;
    private PokerPosition NextPlayer(PokerPosition current) => (PokerPosition)(((int)current + 1) % 5);
    private PokerPosition BigBlind => NextPlayer(SmallBlind);
    private PokerPosition CurrentPlayer;

    private GameState gameState = GameState.StartGame;
    private GameState CurrentGameState 
    { 
        get { return gameState; } 
        set
        {
            GameStateChanged?.Invoke(this, EventArgs.Empty);
            gameState = value;
        }
    }

    [SerializeField]
    public EventHandler GameStateChanged;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // TODO: GameManager.Instance.PokerRound += 1;
        StartCoroutine(FirstPhase());
    }

    IEnumerator FirstPhase()
    {
        PokerManager.Instance.DestroyCards();
        BettingManager.Instance.ResetGame();
        CurrentPlayer = NextPlayer(BigBlind);
        foreach (PokerPosition player in Enum.GetValues(typeof(PokerPosition)))
        {
            if (player == PokerPosition.Table) continue;
            ActivePlayers.Add(player);
        }
        
        // Rotation of the button
        SmallBlind = NextPlayer(SmallBlind);
        BettingManager.Instance.BetAmount(SmallBlind, BettingManager.MINIMUM_BET);
        BettingManager.Instance.SubmitBet(SmallBlind);
        BettingManager.Instance.BetAmount(BigBlind, BettingManager.MINIMUM_BET * 2);
        BettingManager.Instance.SubmitBet(BigBlind);

        PokerManager.Instance.DealCards();
        PokerVisualManager.Instance.OffsetCardsInHands();

        CurrentGameState = GameState.Preflop;
        while (!BettingManager.Instance.AreEqualBets(ActivePlayers))
        {
            foreach (PokerPosition player in Enum.GetValues(typeof(PokerPosition)))
            {
                if (player == PokerPosition.Table) continue;
                if (player != PokerPosition.Joker) continue; // PokerAction action = NPCManager.Instance.GetAction(PokerManager.Instance.GetHand(player))

                awaitingPlayer = true;
                // TODO: Add visual cue to let player know
                yield return new WaitUntil(() => !awaitingPlayer);
            }
        }
        // TODO: Include NPC actions
    }
}
