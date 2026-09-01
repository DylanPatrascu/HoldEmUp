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

    public bool PausedForAnimationEvents { get; private set; } = false;
    public void SetPausedForAnimationEvents(bool toggle) => PausedForAnimationEvents = toggle;

    public class PokerEvent : EventArgs
    {
        // PlayerAction
        public PokerPosition Player;
        public PokerAction Action;
        public int Amount;
        public bool IsBluffing;

        public PokerEvent(PokerPosition Player, PokerAction Action, int Amount, bool IsBluffing)
        {
            this.Player = Player;
            this.Action = Action;
            this.Amount = Amount;
            this.IsBluffing = IsBluffing;
        }
    }

    public EventHandler<PokerEvent> PerformedPlayerAction;

    private bool isEndingHand = false;

    public static PokerGameManager Instance { get; private set; } 

    public bool awaitingPlayer { get; private set; } = false;
    public List<PokerPosition> ActivePlayers = new();
    public PokerPosition NextPlayer(PokerPosition current) => (PokerPosition)(((int)current + 1) % 5);
    public PokerPosition SmallBlind = PokerPosition.Spade;
    private PokerPosition BigBlind => NextPlayer(SmallBlind);
    private PokerPosition CurrentPlayer;

    private GameState gameState = GameState.StartGame;
    public EventHandler GameStateChanged;
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

    private IEnumerator EndHandTransition()
    {
        if (isEndingHand) yield break;
        isEndingHand = true;

        CurrentGameState = GameState.EndGame;
        PokerVisualManager.Instance.ResetNpcBetUI();
        yield return new WaitForSeconds(6);

        if (GameManager.Instance == null)
        {
            yield break;
        }

        if (GameManager.Instance.PlayerBalance <= 0)
        {
            GameManager.Instance.LoadGameOverScene(false);
            yield break;
        }

        if (GameManager.Instance.GameRound >= 5)
        {
            GameManager.Instance.LoadGameOverScene(true);
            yield break;
        }

        GameManager.Instance.Fire(Trigger.ToClub);
    }

    IEnumerator MainGame()
    {
        CurrentGameState = GameState.Preflop;
        yield return StartCoroutine(RunSafely(PreflopPhase(), "PreflopPhase"));
        if (CurrentGameState == GameState.EndGame)
        {
            yield return StartCoroutine(EndHandTransition());
            yield break;
        }

        CurrentGameState = GameState.Postflop;
        yield return StartCoroutine(RunSafely(PostFlopPhase(3), "PostFlopPhase(Flop)"));
        if (CurrentGameState == GameState.EndGame)
        {
            yield return StartCoroutine(EndHandTransition());
            yield break;
        }

        //Turn
        yield return StartCoroutine(RunSafely(PostFlopPhase(1), "PostFlopPhase(Turn)"));
        if (CurrentGameState == GameState.EndGame)
        {
            yield return StartCoroutine(EndHandTransition());
            yield break;
        }

        //River
        yield return StartCoroutine(RunSafely(PostFlopPhase(1), "PostFlopPhase(River)"));
        if (CurrentGameState == GameState.EndGame)
        {
            yield return StartCoroutine(EndHandTransition());
            yield break;
        }

        //Showdown
        try
        {
            PokerManager.Instance.CheckWin();
            PokerVisualManager.Instance.RevealHands();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PokerGameManager] Exception during CheckWin (Showdown): {e}");
        }

        yield return StartCoroutine(EndHandTransition());
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

    private void ResolveEarlyFoldWin()
    {
        if (ActivePlayers.Count != 1)
        {
            Debug.LogWarning($"[PokerGameManager] Early fold resolution skipped: active players = {ActivePlayers.Count}. Need exactly 1 player left.");
            return;
        }

        List<PokerPosition> winnerList = new List<PokerPosition>(ActivePlayers);
        var activePlayers = Enum.GetValues(typeof(PokerPosition))
            .Cast<PokerPosition>()
            .Where(player => player != PokerPosition.Table)
            .ToList();

        PokerVisualManager.Instance.SyncActivePlayerUI(ActivePlayers);
        BettingManager.Instance.SubmitBets(activePlayers);
        BettingManager.Instance.AwardPot(winnerList);

        CurrentGameState = GameState.EndGame;
        Debug.Log($"[PokerGameManager] Early fold resolution: {string.Join(", ", winnerList)} wins the pot.");
        StartCoroutine(EndHandTransition());
    }

    private void ResetCharacterSprites()
    {
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemy.SetToStatic();
        }
    }

    IEnumerator BettingRound()
    {
        ResetCharacterSprites();

        bool isFirstRound = true;
        int maxRoundIterations = Mathf.Max(10, ActivePlayers.Count * 8);
        int iterationCount = 0;

        while (!BettingManager.Instance.AreEqualBets(ActivePlayers) || isFirstRound)
        {
            if (ActivePlayers.Count == 1)
            {
                ResolveEarlyFoldWin();
                yield break;
            }

            iterationCount++;
            if (iterationCount > maxRoundIterations)
            {
                Debug.LogError($"[PokerGameManager] Betting round exceeded safe limit ({maxRoundIterations}). Forcing round close.");
                break;
            }

            BettingVisualManager.Instance.SpawnChips(BettingManager.Instance.PlayerBet, ChipLocation.Table, true);
            BettingVisualManager.Instance.SpawnChips(GameManager.Instance.PlayerBalance, ChipLocation.Stack);

            bool anyActionThisPass = false;
            foreach (PokerPosition player in Enum.GetValues(typeof(PokerPosition)))
            {
                if (player == PokerPosition.Table) continue;
                if (!ActivePlayers.Contains(player)) continue; // folded players don't act again
 
                CurrentPlayer = player;
                anyActionThisPass = true;
 
                if (player != PokerPosition.Joker)
                {
                    PokerAction action = PokerAction.Check;
                    int amount = 0;
                    bool isBluffing = false;
                    bool validAction = false;

                    for (int attempt = 0; attempt < 5; attempt++)
                    {
                        List<PlayingCard> hand = PokerManager.Instance.GetHand(player);
                        GameStateChanged?.Invoke(this, EventArgs.Empty); // Done this to force NPCPlayerAction to switch
                        (action, amount, isBluffing) = NPCManager.Instance.GetAction(PokerManager.Instance.communityCards, hand, player, ActivePlayers);

                        if (SubmitAction(player, action, amount))
                        {
                            validAction = true;
                            break;
                        }

                        Debug.LogWarning($"[PokerGameManager] NPC action rejected for {player}: {action} amount={amount}. Retry {attempt + 1}/5.");
                    }

                    if (!validAction)
                    {
                        int myBet = BettingManager.Instance.GetBet(player);
                        int highestBet = BettingManager.Instance.GetHighestBet(ActivePlayers);
                        int amountToCall = Mathf.Max(0, highestBet - myBet);
                        action = amountToCall > 0 ? PokerAction.Call : PokerAction.Check;
                        amount = amountToCall;
                        Debug.LogWarning($"[PokerGameManager] Falling back to safe NPC action for {player}: {action} amount={amount}.");
                        SubmitAction(player, action, amount);
                    }

                    if (ActivePlayers.Count == 1)
                    {
                        ResolveEarlyFoldWin();
                        yield break;
                    }

                    SetPausedForAnimationEvents(true);
                    var actionEvent = new PokerEvent(player, action, amount, isBluffing);
                    Debug.Log($"[PokerGameManager] NPC action: {player} {action} amount={amount} bluff={isBluffing}");
                    PerformedPlayerAction?.Invoke(this, actionEvent);
                    yield return new WaitUntil(() => !PausedForAnimationEvents);
                    continue;
                }

                SetAwaitingPlayer(true);
                GameStateChanged?.Invoke(this, EventArgs.Empty); // Done this to force PlayerAction to switch
                Debug.Log("[PokerGameManager] Waiting for player input.");
                yield return new WaitUntil(() => !awaitingPlayer);
                yield return new WaitUntil(() => !PausedForAnimationEvents);
                isFirstRound = false;

                if (ActivePlayers.Count == 1)
                {
                    ResolveEarlyFoldWin();
                    yield break;
                }
            }

            if (!anyActionThisPass)
            {
                Debug.LogError("[PokerGameManager] Betting round made no progress this pass. Breaking to avoid infinite loop.");
                break;
            }
        }

        if (ActivePlayers.Count == 1)
        {
            ResolveEarlyFoldWin();
            yield break;
        }

        BettingManager.Instance.SubmitBets(ActivePlayers);
    }

    public bool SubmitAction(PokerPosition player, PokerAction action, int amount)
    {
        try
        {
            switch (action)
            {
                case PokerAction.Fold:
                    ActivePlayers.Remove(player);
                    PokerVisualManager.Instance.HidePlayerBetUI(player);
                    Debug.Log($"[PokerGameManager] {player} folds.");
                    return true;
 
                case PokerAction.Check:
                    Debug.Log($"[PokerGameManager] {player} checks.");
                    return true;
 
                case PokerAction.Call:
                case PokerAction.Bet:
                case PokerAction.Raise:
                    if (amount <= 0)
                    {
                        Debug.LogWarning($"[PokerGameManager] Invalid move for {player}: {action} amount={amount}.");
                        return false;
                    }

                    int myBet = BettingManager.Instance.GetBet(player);
                    int highestBet = BettingManager.Instance.GetHighestBet(ActivePlayers);
                    int amountToCall = Mathf.Max(0, highestBet - myBet);

                    if (action == PokerAction.Call && amount != amountToCall)
                    {
                        Debug.LogWarning($"[PokerGameManager] Invalid call for {player}: amount={amount}, needed={amountToCall}.");
                        return false;
                    }

                    if ((action == PokerAction.Bet || action == PokerAction.Raise) && amount + myBet < highestBet)
                    {
                        Debug.LogWarning($"[PokerGameManager] Invalid bet/raise for {player}: amount={amount}, current={myBet}, highest={highestBet}.");
                        return false;
                    }

                    Debug.Log($"[PokerGameManager] {player} {action.ToString().ToLower()}s {amount}.");
                    if (player != PokerPosition.Joker || action == PokerAction.Call)
                        return BettingManager.Instance.BetAmount(player, amount, ActivePlayers);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[PokerGameManager] Exception in SubmitAction for {player}, action {action}, amount {amount}: {e}");
        }
        return true;
    }

    IEnumerator PreflopPhase()
    {
        Debug.Log("[PokerGameManager] PreflopPhase started.");
        try
        {
            PokerManager.Instance.DestroyCards();
            BettingManager.Instance.ResetGame();

            for (int i = 0; i < GameManager.Instance.GameRound - 1; i++)
                SmallBlind = NextPlayer(SmallBlind);

            CurrentPlayer = NextPlayer(BigBlind);

            ActivePlayers.Clear();
            foreach (PokerPosition player in Enum.GetValues(typeof(PokerPosition)))
            {
                if (player == PokerPosition.Table) continue;
                ActivePlayers.Add(player);
            }

            PokerVisualManager.Instance.ResetNpcBetUI();
            PokerVisualManager.Instance.SyncActivePlayerUI(ActivePlayers);

            // Rotation of the button
            SmallBlind = NextPlayer(SmallBlind);
            BettingManager.Instance.BetAmount(SmallBlind, BettingManager.MINIMUM_BET, ActivePlayers);
            BettingManager.Instance.BetAmount(BigBlind, BettingManager.MINIMUM_BET * 2, ActivePlayers);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PokerGameManager] There was an exception in PreflopPhase: {e}");
        }

        yield return PokerManager.Instance.DealCards();

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

        yield return StartCoroutine(PokerVisualManager.Instance.DealToCommunityCards());

        //starts following betting round
        yield return StartCoroutine(RunSafely(BettingRound(), "PostFlopPhase.BettingRound"));
    }

    //ui button method
    public void SetAwaitingPlayer(bool toggle)
    {
        awaitingPlayer = toggle;
    }
}
