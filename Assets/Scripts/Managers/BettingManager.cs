using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BettingManager : MonoBehaviour
{
    public static BettingManager Instance { get; private set; }
    
    [SerializeField] 
    private Dictionary<PokerPosition, int> currentBets;

    public int GetBet(PokerPosition player) => currentBets.TryGetValue(player, out int bet) ? bet : 0;
    public int GetHighestBet(List<PokerPosition> players) => players.Select(GetBet).DefaultIfEmpty(0).Max();
    public bool AreEqualBets(List<PokerPosition> players)
    {
        if (players == null || players.Count == 0)
            return true;

        List<PokerPosition> activePlayers = players
            .Where(player => player != PokerPosition.Table)
            .Distinct()
            .ToList();

        if (activePlayers.Count == 0)
            return true;

        int referenceBet = GetBet(activePlayers[0]);
        return activePlayers.All(player => GetBet(player) == referenceBet);
    }

    public int PlayerBet => currentBets.TryGetValue(PokerPosition.Joker, out int bet) ? bet : 0;
    public int Pot => pot;

    [SerializeField] private int pot = 0;

    public const int MINIMUM_BET = 1;
    public const int MAXIMUM_BET = 30;
    public const int STARTING_BALANCE = 100;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentBets = new();
    }

    public void ResetGame()
    {
        pot = 0;
        GameManager.Instance.PlayerBalance = STARTING_BALANCE;

        // Reset bets
        foreach (PokerPosition player in Enum.GetValues(typeof(PokerPosition)))
        {
            if (player == PokerPosition.Table) continue;
            currentBets[player] = 0;
        }
    }

    // usable for player
    public void ResetBet()
    {
        GameManager.Instance.PlayerBalance += PlayerBet;
        currentBets[PokerPosition.Joker] = 0;
        BettingVisualManager.Instance.UpdateBet(PlayerBet);
        AudioManager.Instance.PlayAudioClip(AudioSnippet.PokerChip);
    }

    // usable by UI and NPCS
    public bool BetAmount(PokerPosition player, int amount, List<PokerPosition> activePlayers)
    {
        if (amount <= 0)
            return false;

        int currentPlayerBet = GetBet(player);
        int totalAfterBet = currentPlayerBet + amount;

        Debug.Log($"{player} just wanted to bet {amount} (current total {currentPlayerBet})");

        if (totalAfterBet > MAXIMUM_BET)
            return false;

        if (player == PokerPosition.Joker)
        {
            if (GameManager.Instance.PlayerBalance < amount)
                return false;
            GameManager.Instance.PlayerBalance -= amount;
        }

        AudioManager.Instance.PlayAudioClip(AudioSnippet.PokerChip);
        currentBets[player] = totalAfterBet;
        Debug.Log($"{player} added {amount} to their current bet");
        BettingVisualManager.Instance.UpdateBet(PlayerBet);
        return true;
    }
    
    // usable by UI and NPCS
    public bool RemoveAmount(int amount)
    {
        // if sufficient currentbet amount
        if (PlayerBet >= amount)
        {
            currentBets[PokerPosition.Joker] -= amount;
            GameManager.Instance.PlayerBalance += amount;
            BettingVisualManager.Instance.UpdateBet(PlayerBet);
            AudioManager.Instance.PlayAudioClip(AudioSnippet.PokerChip);
            return true;
        }
        return false;
    }

    public void SubmitBets(List<PokerPosition> players)
    {
        foreach (PokerPosition player in players)
        {
            if (player == PokerPosition.Table) continue;
            pot += currentBets[player];
            currentBets[player] = 0;
            BettingVisualManager.Instance.UpdateBet(PlayerBet);
            AudioManager.Instance.PlayAudioClip(AudioSnippet.PokerChip);
        }
    }

    public void AwardPot(List<PokerPosition> winners)
    {
        if (winners == null || winners.Count == 0)
        {
            pot = 0;
            return;
        }

        if (pot <= 0)
        {
            pot = 0;
            return;
        }

        int winningsPerWinner = pot / winners.Count;
        int remainder = pot % winners.Count;

        if (winners.Contains(PokerPosition.Joker))
        {
            GameManager.Instance.PlayerBalance += winningsPerWinner + remainder;
        }

        pot = 0;
    }

}
