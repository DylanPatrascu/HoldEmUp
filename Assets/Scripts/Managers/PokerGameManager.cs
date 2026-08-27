using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokerGameManager : MonoBehaviour
{
    public enum GameState
    {
        StartGame,
        Preflop, // Betting without check
        Turn, // Betting with check
        EndGame
    }

    public static PokerGameManager Instance;

    [SerializeField] private bool awaitingPlayer = false;
    public List<PokerPosition> ActivePlayers;
    private PokerPosition SmallBlind = PokerPosition.Heart;
    private PokerPosition NextPlayer(PokerPosition current) => (PokerPosition)(((int)current + 1) % 5);
    private PokerPosition BigBlind => NextPlayer(SmallBlind);
    private PokerPosition CurrentPlayer;

    private GameState gameState = GameState.StartGame;
    public GameState CurrentGameState 
    { 
        get { return gameState; } 
        set
        {
            gameState = value;
            GameStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [SerializeField]
    public EventHandler GameStateChanged;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ActivePlayers = new List<PokerPosition>();
    }

    void Start()
    {    
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
        Debug.Log("h");
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
                CurrentPlayer = player;
                if (player != PokerPosition.Joker) continue; // PokerAction action = NPCManager.Instance.GetAction(PokerManager.Instance.GetHand(player))

                awaitingPlayer = true;
                // TODO: Add visual cue to let player know
                // TODO: Player CANNOT check
                yield return new WaitUntil(() => !awaitingPlayer);
            }
        }
        // TODO: Include NPC actions
    }

    public void SetAwaitingPlayer(bool toggle)
    {
        awaitingPlayer = toggle;
    }
}
