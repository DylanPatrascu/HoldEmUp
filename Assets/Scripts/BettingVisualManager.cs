using TMPro;
using UnityEngine;

public class BettingVisualManager : MonoBehaviour
{
    public static BettingVisualManager Instance { get; private set; }

    [SerializeField] private BettingManager bettingManager;
    [SerializeField] private TMP_Text currentBetText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayerBet(int amount)
    {
        bettingManager.BetAmount(PokerPosition.Joker, amount);
    }

    public void PlayerAllIn()
    {
        bettingManager.AllIn(PokerPosition.Joker);
    }
    public void PlayerRemove(int amount)
    {
        bettingManager.RemoveAmount(PokerPosition.Joker, amount);
    }

    public void SubmitBet()
    {
        bettingManager.SubmitBet();
    }

    public void PlayerResetBet()
    {
        bettingManager.ResetBet();
    }

    public void UpdateBet(int bet)
    {
        currentBetText.text = bet.ToString();
    }
}
