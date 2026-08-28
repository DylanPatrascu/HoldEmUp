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
        Postflop, // Betting with check so it includes Flop, Turn and River phases
        EndGame
    }

    public static PokerGameManager Instance;
    public bool awaitingPlayer { get; private set; } = false;
    private List<PokerPosition> ActivePlayers;
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
        StartCoroutine(MainGame());
    }

    IEnumerator MainGame()
    {
        CurrentGameState = GameState.Preflop;
        yield return StartCoroutine(PreflopPhase());
        CurrentGameState = GameState.Postflop;
        yield return StartCoroutine(PostFlopPhase(3));

        //Turn
        yield return StartCoroutine(PostFlopPhase(1));

        //River
        yield return StartCoroutine(PostFlopPhase(1));

        //Showdown
        PokerManager.Instance.CheckWin();

    }

    IEnumerator BettingRound()
    {
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
    }

    IEnumerator PreflopPhase()
    {
        PokerManager.Instance.DestroyCards();
        BettingManager.Instance.ResetGame();
        // TODO: GameManager.Instance.PokerRound += 1; (shift small blind by how many games of poker youve played
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

        yield return StartCoroutine(BettingRound());
    }

    IEnumerator PostFlopPhase(int numCards)
    {
        PokerManager.Instance.BurnCard();
        PokerManager.Instance.DrawCard(PokerManager.Instance.communityCards, PokerPosition.Table, numCards);

        //starts folling betting round
        yield return StartCoroutine(BettingRound());
    }

    //ui button method
    public void SetAwaitingPlayer(bool toggle)
    {
        awaitingPlayer = toggle;
    }
}
