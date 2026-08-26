using System.Collections.Generic;
using UnityEngine;

public class BettingManager : MonoBehaviour
{
    [Header("Chip Balances")]
    [SerializeField] private int jokerBalance;
    [SerializeField] private int clubBalance;
    [SerializeField] private int spadeBalance;
    [SerializeField] private int heartBalance;
    [SerializeField] private int diamondBalance;

    public List<int> allBalances;

    public int currentBet;
    public PokerPosition currentBetter;

    private void Start()
    {
        allBalances = new List<int> { jokerBalance, clubBalance, spadeBalance, heartBalance, diamondBalance };
    }
    // usable for player
    public void ResetBet()
    {
        allBalances[(int)PokerPosition.Joker] += currentBet;
        currentBet = 0;
        BettingVisualManager.Instance.UpdateBet(currentBet);
    }

    // usable by UI and NPCS
    public void BetAmount(PokerPosition player, int amount)
    {
        // if sufficient balance
        if (allBalances[(int)player] - amount >= 0)
        {
            currentBetter = player;
            currentBet += amount;
            allBalances[(int)player] -= amount;
            BettingVisualManager.Instance.UpdateBet(currentBet);
        }
    }
    
    // all in uses the remainder of your balance
    public void AllIn(PokerPosition player)
    {
        currentBet += allBalances[(int)player];
        allBalances[(int)player] = 0;
        currentBetter = player;
        BettingVisualManager.Instance.UpdateBet(currentBet);
    }

    // usable by UI and NPCS
    public void RemoveAmount(PokerPosition player, int amount)
    {
        // if sufficient currentbet amount
        if (currentBet - amount >= 0)
        {
            currentBetter = player;
            currentBet -= amount;
            allBalances[(int)player] += amount;
            BettingVisualManager.Instance.UpdateBet(currentBet);
        }
    }

    public void SubmitBet()
    {
        Debug.Log($"{currentBetter} just bet ${currentBet}");
        currentBet = 0;
        BettingVisualManager.Instance.UpdateBet(currentBet);
    }

}
