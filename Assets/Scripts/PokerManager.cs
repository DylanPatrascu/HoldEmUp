using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokerManager : MonoBehaviour
{
    [Header("Prefabs & Transforms")]
    [SerializeField] private GameObject playingCardPrefab;
    [SerializeField] private GameObject tablePlayingCardPrefab;

    [SerializeField] private GameObject playerHandTransform;
    [SerializeField] private GameObject opponentHandTransform;
    [SerializeField] private GameObject tableCardTransform;

    [Header("Cards and Hands")]
    [SerializeField] private List<PlayingCard> playerHand;
    [SerializeField] private List<PlayingCard> opponentHand;
    [SerializeField] private List<PlayingCard> communityCards;
    private List<PlayingCard> deckList;
    private List<PlayingCard> runtimeDeck;

    private void Awake()
    {
        deckList = Resources.LoadAll<PlayingCard>("PlayingCards").ToList();
    }
    private void Start()
    {
        ResetGame();
    }

    public void ResetGame()
    {
        DestroyCards();
        
        DrawCard(playerHand, true);
        DrawCard(opponentHand, false);
        DrawCard(playerHand, true);
        DrawCard(opponentHand, false);
        OffsetCardsInHand();

        //Flop
        PlayCommunityCard(3);
        //Turn
        PlayCommunityCard(1);
        //River
        PlayCommunityCard(1);
    }

    public void DrawCard(List<PlayingCard> hand, bool isPlayer)
    {
        //Debug.Log($"Drew {runtimeDeck[0].name}");
        hand.Add(runtimeDeck[0]);
        VisualPlayingCard card = Instantiate(playingCardPrefab, isPlayer ? playerHandTransform.transform : opponentHandTransform.transform).GetComponent<VisualPlayingCard>();
        card.PopulateData(runtimeDeck[0]);
        runtimeDeck.Remove(runtimeDeck[0]);
    }

    public void PlayCommunityCard(int numCards)
    {
        for (int i = 0; i < numCards; i++)
        {
            //Debug.Log($"Played {runtimeDeck[0].name}");
            communityCards.Add(runtimeDeck[0]);
            VisualPlayingCard card = Instantiate(tablePlayingCardPrefab, tableCardTransform.transform).GetComponent<VisualPlayingCard>();
            card.gameObject.transform.localPosition += new Vector3((tableCardTransform.transform.childCount - 1) * 1, 0, 0);
            card.PopulateData(runtimeDeck[0]);
            runtimeDeck.Remove(runtimeDeck[0]);
        }
    }

    public void OffsetCardsInHand()
    {
        playerHandTransform.transform.GetChild(0).localPosition += new Vector3(-1, 0, 0);
        playerHandTransform.transform.GetChild(1).localPosition += new Vector3(1, 0, 0);

        opponentHandTransform.transform.GetChild(0).localPosition += new Vector3(-1, 0, 0);
        opponentHandTransform.transform.GetChild(1).localPosition += new Vector3(1, 0, 0);
    }


    private void DestroyCards()
    {
        playerHand.Clear();
        opponentHand.Clear();
        communityCards.Clear();
        runtimeDeck.Clear();

        runtimeDeck = new List<PlayingCard>(deckList);
        HelperMethods.Shuffle(runtimeDeck);

        for (int i = playerHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            playerHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = opponentHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            opponentHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = tableCardTransform.transform.childCount - 1; i >= 0; i--)
        {
            tableCardTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

    public void CheckWin()
    {
        PokerHand playerScore = EvaluateScore(communityCards, playerHand);
        PokerHand opponentScore = EvaluateScore(communityCards, opponentHand);
        
        Debug.Log("Player: " + playerScore);
        Debug.Log("Opponent" + opponentScore);
        
        // Lower enum value = stronger hand
        if (playerScore < opponentScore)
        {
            Debug.Log("PlayerWins: " + playerScore);
        }
        else if (playerScore == opponentScore)
        {
            Debug.Log("Draw: " + playerScore);
        }
        else
        {
            Debug.Log("OpponentWins" + opponentScore);
        }
    }

    public PokerHand EvaluateScore(List<PlayingCard> communityCards, List<PlayingCard> hand)
    {
        List<PlayingCard> cards = new List<PlayingCard>(communityCards);
        cards.AddRange(hand);

        if (cards.Count < 5)
        {
            Debug.LogWarning("Not enough cards to evaluate poker hand.");
            return PokerHand.HighCard;
        }

        PokerHand bestHand = PokerHand.HighCard;

        var uniqueHands = GetCombinations(cards, 5).ToList();

        
        for (int i = 0; i < uniqueHands.Count; i++)
        {
            PokerHand currentHand = EvaluateFiveCards(uniqueHands[i].ToList());

            // Lower enum value = stronger hand
            if (currentHand < bestHand)
            {
                Debug.Log($"NEW BEST: {currentHand} | " + $"{string.Join(", ", uniqueHands[i].ToList())}");
                bestHand = currentHand;
            }
        }

        return bestHand;
    }

    

    // Algorithm I found to get combinations
    static IEnumerable<IEnumerable<T>> GetCombinations<T>(IEnumerable<T> list, int length)
    {
        if (length == 1)
        {
            return list.Select(item => new T[] { item });
        }

        return list.SelectMany((item, index) => GetCombinations(list.Skip(index + 1), length - 1).Select(c => new T[] { item }.Concat(c)));
    }

    private PokerHand EvaluateFiveCards(List<PlayingCard> cards)
    {
        cards = cards.OrderByDescending(card => card.cardValue).ToList();

        Dictionary<int, int> valueCounts = cards.GroupBy(card => card.cardValue).ToDictionary(group => group.Key, group => group.Count());
        
        bool isFlush = IsFlush(cards);
        bool isStraight = IsStraight(cards);

        // Royal Flush
        if (isFlush && isStraight && cards[0].cardValue == 14)
        {
            return PokerHand.RoyalFlush;
        }

        // Straight Flush
        if (isFlush && isStraight)
        {
            return PokerHand.StraightFlush;
        }

        // Four of a Kind
        if (valueCounts.Values.Any(count => count == 4))
        {
            return PokerHand.FourOfAKind;
        }

        // Full House
        if (valueCounts.Values.Contains(3) && valueCounts.Values.Contains(2))
        {
            return PokerHand.FullHouse;
        }

        // Flush
        if (isFlush)
        {
            return PokerHand.Flush;
        }

        // Straight
        if (isStraight)
        {
            return PokerHand.Straight;
        }

        // Three of a Kind
        if (valueCounts.Values.Any(count => count == 3))
        {
            return PokerHand.ThreeOfAKind;
        }

        // Two Pair
        if (valueCounts.Values.Count(count => count == 2) >= 2)
        {
            return PokerHand.TwoPair;
        }

        // One Pair
        if (valueCounts.Values.Any(count => count == 2))
        {
            return PokerHand.OnePair;
        }

        // High Card
        return PokerHand.HighCard;
    }

    private bool IsFlush(List<PlayingCard> cards)
    {
        return cards.All(card => card.cardSuit == cards[0].cardSuit);
    }

    private bool IsStraight(List<PlayingCard> cards)
    {
        List<int> values = cards.Select(card => card.cardValue).Distinct().OrderByDescending(value => value).ToList();

        if (values.Count != 5)
        {
            return false;
        }

        // Normal straight
        // Example: 10, 9, 8, 7, 6
        if (values[0] - values[4] == 4)
        {
            return true;
        }

        // Ace-low straight
        // A, 2, 3, 4, 5
        if (values.Contains(14) && values.Contains(2) && values.Contains(3) && values.Contains(4) && values.Contains(5))
        {
            return true;
        }

        return false;
    }

}
