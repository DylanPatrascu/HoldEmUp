using TMPro;
using UnityEngine;

public class BettingVisualManager : MonoBehaviour
{
    public static BettingVisualManager Instance { get; private set; }

    [SerializeField] private BettingManager bettingManager;
    [SerializeField] private TMP_Text currentBetText;

    [SerializeField] private Transform OneDollarChipParent;
    [SerializeField] private Transform TenDollarChipParent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < TenDollarChipParent.childCount; i++)
        {
            TenDollarChipParent.GetChild(i).gameObject.SetActive(false);
        }

        for (int i = 0; i < OneDollarChipParent.childCount; i++)
        {
            OneDollarChipParent.GetChild(i).gameObject.SetActive(false);
        }
    }
    public void PlayerBet(int amount)
    {
        bettingManager.BetAmount(PokerPosition.Joker, amount);
        UpdateVisibleChips();
    }

    public void PlayerAllIn()
    {
        bettingManager.AllIn(PokerPosition.Joker);
    }
    public void PlayerRemove(int amount)
    {
        bettingManager.RemoveAmount(PokerPosition.Joker, amount);
        UpdateVisibleChips();
    }

    public void SubmitBet()
    {
        bettingManager.SubmitBet();
        UpdateVisibleChips();
    }

    public void PlayerResetBet()
    {
        bettingManager.ResetBet();
        UpdateVisibleChips();
    }

    public void UpdateBet(int bet)
    {
        currentBetText.text = bet.ToString();
    }

    public (int, int) GetChipDistribution()
    {
        int balance = bettingManager.GetCurrentBet();
        return ((balance - balance % 10) / 10, balance % 10);
    }

    public void UpdateVisibleChips()
    {
        (int, int) chipCounts = GetChipDistribution();
        Debug.Log($"{chipCounts.Item1},{chipCounts.Item2}");
        for (int i = 0; i < TenDollarChipParent.childCount; i++) {
            TenDollarChipParent.GetChild(i).gameObject.SetActive(i < chipCounts.Item1);
        }

        for (int i = 0; i < OneDollarChipParent.childCount; i++)
        {
            OneDollarChipParent.GetChild(i).gameObject.SetActive(i < chipCounts.Item2);
        }
    }
}
