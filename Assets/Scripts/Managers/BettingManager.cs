using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BettingManager : MonoBehaviour
{
    public static BettingManager Instance { get; private set; }

    [SerializeField] private int maxBet;

    [Header("Chip Details")]
    [SerializeField] 
    private int playerBalance; // TODO: Retain using GameManager
    
    [SerializeField] 
    private Dictionary<PokerPosition, int> currentBets;

    public bool AreEqualBets(List<PokerPosition> players) => players.All(player => currentBets[player] == currentBets[players[0]]);
    public int GetBet(PokerPosition player) => currentBets.TryGetValue(player, out int bet) ? bet : 0;
    public int GetHighestBet(List<PokerPosition> players) => players.Select(GetBet).DefaultIfEmpty(0).Max();
    public int PlayerBet => currentBets[PokerPosition.Joker];

    [SerializeField] private int pot = 0;

    public const int MINIMUM_BET = 1;
    public const int MAXIMUM_BET = 30;
    public const int STARTING_BALANCE = 100;

    private void Awake()
    {
        currentBets = new Dictionary<PokerPosition, int>();
    }
    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ResetGame()
    {
        pot = 0;
        playerBalance = STARTING_BALANCE;

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
        playerBalance += PlayerBet;
        currentBets[PokerPosition.Joker] = 0;
        BettingVisualManager.Instance.UpdateBet(currentBets[PokerPosition.Joker]);
    }

    // usable by UI and NPCS
    public void BetAmount(PokerPosition player, int amount)
    {
        if (currentBets[player] + amount > MAXIMUM_BET) return;

        if (player == PokerPosition.Joker)
        {
            // TODO: Add visual cue that player has insufficient money
            if (playerBalance - amount < 0) return;
            playerBalance -= amount;
            BettingVisualManager.Instance.UpdateBet(currentBets[player]);
        }

        currentBets[player] += amount;
        Debug.Log($"{player} bet {amount}");
    }
    
    // usable by UI and NPCS
    public void RemoveAmount(int amount)
    {
        // if sufficient currentbet amount
        if (PlayerBet >= amount)
        {
            currentBets[PokerPosition.Joker] -= amount;
            playerBalance += amount;
            BettingVisualManager.Instance.UpdateBet(currentBets[PokerPosition.Joker]);
        }
    }

    public void SubmitBet(PokerPosition player)
    {
        if (player == PokerPosition.Table) return;
        pot += currentBets[player];
        currentBets[player] = 0;
        BettingVisualManager.Instance.UpdateBet(currentBets[PokerPosition.Joker]);
    }
}
