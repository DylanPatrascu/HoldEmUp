using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public PokerAction GetAction(List<PlayingCard> communityCards, List<PlayingCard> hand, PokerPosition personality)
    {
        return PokerAction.Fold;
    }
}