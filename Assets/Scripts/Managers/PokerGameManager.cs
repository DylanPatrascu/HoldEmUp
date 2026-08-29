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

    public static PokerGameManager Instance { get; private set; } 

    public bool awaitingPlayer { get; private set; } = false;
    public List<PokerPosition> ActivePlayers = new();
    private PokerPosition NextPlayer(PokerPosition current) => (PokerPosition)(((int)current + 1) % 5);
    private PokerPosition SmallBlind = PokerPosition.Spade;
    private PokerPosition BigBlind => NextPlayer(SmallBlind);
    private PokerPosition CurrentPlayer;

    private GameState gameState = GameState.StartGame;
    public GameState CurrentGameState 
    { 
        get { return gameState; } 
        set
        {
            Debug.Log($"[PokerGameManager] State: {value}");
            gameState = value;
            GameStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public EventHandler GameStateChanged;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {    
        StartCoroutine(MainGame());
    }

    IEnumerator MainGame()
    {
        CurrentGameState = GameState.Preflop;
        yield return StartCoroutine(RunSafely(PreflopPhase(), "PreflopPhase"));

        CurrentGameState = GameState.Postflop;
        yield return StartCoroutine(RunSafely(PostFlopPhase(3), "PostFlopPhase(Flop)"));

        //Turn
        yield return StartCoroutine(RunSafely(PostFlopPhase(1), "PostFlopPhase(Turn)"));

        //River
        yield return StartCoroutine(RunSafely(PostFlopPhase(1), "PostFlopPhase(River)"));

        //Showdown
        try
        {
            PokerManager.Instance.CheckWin();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PokerGameManager] Exception during CheckWin (Showdown): {e}");
        }

        CurrentGameState = GameState.EndGame;
    }

    private IEnumerator RunSafely(IEnumerator routine, string phaseName)
    {
        while (true)
        {
            object current;
            try
            {
                if (!routine.MoveNext())
                {
                    yield break;
                }
                current = routine.Current;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PokerGameManager] Exception inside {phaseName}: {e}");
                yield break;
            }
            yield return current;
        }
    }

    IEnumerator BettingRound()
    {
        do
        {
            BettingVisualManager.Instance.SpawnChips(BettingManager.Instance.PlayerBet, ChipLocation.Table, true);
            BettingVisualManager.Instance.SpawnChips(GameManager.Instance.PlayerBalance, ChipLocation.Stack);
            foreach (PokerPosition player in Enum.GetValues(typeof(PokerPosition)))
            {
                if (player == PokerPosition.Table) continue;
                if (!ActivePlayers.Contains(player)) continue; // folded players don't act again
 
                CurrentPlayer = player;
 
                if (player != PokerPosition.Joker)
                {
                    try
                    {
                        List<PlayingCard> hand = PokerManager.Instance.GetHand(player);
                        GameStateChanged?.Invoke(this, EventArgs.Empty); // Done this to force NPCPlayerAction to switch
                        (PokerAction action, int amount, bool isBluffing) = NPCManager.Instance.GetAction(PokerManager.Instance.communityCards, hand, player, ActivePlayers);
                        SubmitAction(player, action, amount);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[PokerGameManager] Exception while processing NPC action for {player}: {e}");
                    }
                    continue;
                }

                SetAwaitingPlayer(true);
                GameStateChanged?.Invoke(this, EventArgs.Empty); // Done this to force PlayerAction to switch
                Debug.Log("[PokerGameManager] Waiting for player input.");
                // TODO: Add visual cue to let player know
                yield return new WaitUntil(() => !awaitingPlayer);
            }
        } while (!BettingManager.Instance.AreEqualBets(ActivePlayers));
        BettingManager.Instance.SubmitBets(ActivePlayers);
    }

    public void SubmitAction(PokerPosition player, PokerAction action, int amount)
    {
        try
        {
            switch (action)
            {
                case PokerAction.Fold:
                    ActivePlayers.Remove(player);
                    Debug.Log($"[PokerGameManager] {player} folds.");
                    break;
 
                case PokerAction.Check:
                    Debug.Log($"[PokerGameManager] {player} checks.");
                    break;
 
                case PokerAction.Call:
                case PokerAction.Bet:
                case PokerAction.Raise:
                    BettingManager.Instance.BetAmount(player, amount);
                    Debug.Log($"[PokerGameManager] {player} {action.ToString().ToLower()}s {amount}.");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PokerGameManager] Exception in SubmitAction for {player}, action {action}, amount {amount}: {e}");
        }
    }

    IEnumerator PreflopPhase()
    {
        Debug.Log("[PokerGameManager] PreflopPhase started.");
        try
        {
            PokerManager.Instance.DestroyCards();
            BettingManager.Instance.ResetGame();

            for (int i = 0; i < GameManager.Instance.PokerRound; i++)
                SmallBlind = NextPlayer(SmallBlind);
            GameManager.Instance.PokerRound += 1;

            CurrentPlayer = NextPlayer(BigBlind);

            ActivePlayers.Clear();
            foreach (PokerPosition player in Enum.GetValues(typeof(PokerPosition)))
            {
                if (player == PokerPosition.Table) continue;
                ActivePlayers.Add(player);
            }

            // Rotation of the button
            SmallBlind = NextPlayer(SmallBlind);
            BettingManager.Instance.BetAmount(SmallBlind, BettingManager.MINIMUM_BET);
            BettingManager.Instance.BetAmount(BigBlind, BettingManager.MINIMUM_BET * 2);

            PokerManager.Instance.DealCards();

            PokerVisualManager.Instance.OffsetCardsInHands();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PokerGameManager] There was an exception in PreflopPhase: {e}");
        }

        yield return StartCoroutine(RunSafely(BettingRound(), "PreflopPhase.BettingRound"));
    }

    IEnumerator PostFlopPhase(int numCards)
    {
        Debug.Log($"[PokerGameManager] PostFlopPhase started, drawing {numCards} community card(s).");
        try
        {
            PokerManager.Instance.BurnCard();

            PokerManager.Instance.DrawCard(PokerManager.Instance.communityCards, PokerPosition.Table, numCards);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PokerGameManager] There was an exception in PostFlopPhase (numCards={numCards}): {e}");
        }

        //starts following betting round
        yield return StartCoroutine(RunSafely(BettingRound(), "PostFlopPhase.BettingRound"));
    }

    //ui button method
    public void SetAwaitingPlayer(bool toggle)
    {
        awaitingPlayer = toggle;
    }
}
