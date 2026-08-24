using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokerManager : MonoBehaviour
{
    [Header("Hands")]
    [SerializeField] private List<PlayingCard> playerHand;
    [SerializeField] private List<PlayingCard> clubOpponentHand;
    [SerializeField] private List<PlayingCard> spadeOpponentHand;
    [SerializeField] private List<PlayingCard> heartOpponentHand;
    [SerializeField] private List<PlayingCard> diamondOpponentHand;

    [SerializeField] private List<PlayingCard> communityCards;
    private List<PlayingCard> deckList;
    private List<PlayingCard> runtimeDeck;

    private void Awake()
    {
        deckList = Resources.LoadAll<PlayingCard>("PlayingCards").ToList();
    }
    private void Start()
    {
        runtimeDeck = new List<PlayingCard>(deckList);
        ResetGame();
    }

    public void ResetGame()
    {
        DestroyCards();

        DealCards();

        PokerVisualManager.Instance.OffsetCardsInHands();

        //Flop
        DrawCard(communityCards, PokerPosition.Table, 3);
        //Turn
        DrawCard(communityCards, PokerPosition.Table, 1);
        //River
        DrawCard(communityCards, PokerPosition.Table, 1);
    }

    private void DealCards()
    {
        DrawCard(playerHand, PokerPosition.Joker);
        DrawCard(heartOpponentHand, PokerPosition.Heart);
        DrawCard(spadeOpponentHand, PokerPosition.Spade);
        DrawCard(clubOpponentHand, PokerPosition.Club);
        DrawCard(diamondOpponentHand, PokerPosition.Diamond);
        DrawCard(playerHand, PokerPosition.Joker);
        DrawCard(heartOpponentHand, PokerPosition.Heart);
        DrawCard(spadeOpponentHand, PokerPosition.Spade);
        DrawCard(clubOpponentHand, PokerPosition.Club);
        DrawCard(diamondOpponentHand, PokerPosition.Diamond);
    }

    public void DrawCard(List<PlayingCard> hand, PokerPosition position, int numCards = 1)
    {
        for (int i = 0; i < numCards; i++)
        {
            PlayingCard c = runtimeDeck[0];
            hand.Add(c);
            PokerVisualManager.Instance.SpawnCard(c, position);
            runtimeDeck.Remove(c);
        }
    }

    private void DestroyCards()
    {
        playerHand.Clear();
        clubOpponentHand.Clear();
        diamondOpponentHand.Clear();
        heartOpponentHand.Clear();
        spadeOpponentHand.Clear();
        communityCards.Clear();
        runtimeDeck.Clear();

        // Fresh playing deck
        runtimeDeck = new List<PlayingCard>(deckList);
        HelperMethods.Shuffle(runtimeDeck);

        PokerVisualManager.Instance.DestroyCardVisuals();
    }

    public void CheckWin()
    {
        PokerHand playerScore = EvaluateScore(communityCards, playerHand);
        PokerHand clubOpponentScore = EvaluateScore(communityCards, clubOpponentHand);
        PokerHand heartOpponentScore = EvaluateScore(communityCards, heartOpponentHand);
        PokerHand spadeOpponentScore = EvaluateScore(communityCards, spadeOpponentHand);
        PokerHand diamondOpponentScore = EvaluateScore(communityCards, diamondOpponentHand);

        Debug.Log("Player: " + playerScore);
        Debug.Log("Club Opponent" + clubOpponentScore);
        Debug.Log("Spade Opponent" + spadeOpponentScore);
        Debug.Log("Diamond Opponent" + diamondOpponentScore);
        Debug.Log("Heart Opponent" + heartOpponentScore);

        // Lower enum value = stronger hand
        //if (playerScore < clubOpponentScore)
        //{
        //    Debug.Log("PlayerWins: " + playerScore);
        //}
        //else if (playerScore == clubOpponentScore)
        //{
        //    Debug.Log("Draw: " + playerScore);
        //}
        //else
        //{
        //   Debug.Log("OpponentWins" + clubOpponentScore);
        //}
    }

    public PokerHand EvaluateScore(List<PlayingCard> communityCards, List<PlayingCard> hand)
    {
        List<PlayingCard> cards = new List<PlayingCard>(communityCards);
        // Add hand to the community cards for the total 7 usable cards
        cards.AddRange(hand);

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
            Debug.Log($"royal flush, max card:{cards.Max(c => c.cardValue)}");
            return PokerHand.RoyalFlush;
        }

        // Straight Flush
        if (isFlush && isStraight)
        {
            Debug.Log($"straight flush, max card:{cards.Max(c => c.cardValue)}");
            return PokerHand.StraightFlush;
        }

        // Four of a Kind
        if (valueCounts.Values.Any(count => count == 4))
        {
            Debug.Log($"straight flush, card:{valueCounts.First(pair => pair.Value == 4).Key}");
            return PokerHand.FourOfAKind;
        }

        // Full House
        if (valueCounts.Values.Contains(3) && valueCounts.Values.Contains(2))
        {
            Debug.Log($"full house, cards:{valueCounts.First(pair => pair.Value == 3).Key} {valueCounts.First(pair => pair.Value == 2).Key}");

            return PokerHand.FullHouse;
        }

        // Flush
        if (isFlush)
        {
            Debug.Log($"flush, max card:{cards.Max(c => c.cardValue)}");
            return PokerHand.Flush;
        }

        // Straight
        if (isStraight)
        {
            Debug.Log($"straight, max card:{cards.Max(c => c.cardValue)}");
            return PokerHand.Straight;
        }

        // Three of a Kind
        if (valueCounts.Values.Any(count => count == 3))
        {
            Debug.Log($"three of a kind, cards:{valueCounts.First(pair => pair.Value == 3).Key}");
            return PokerHand.ThreeOfAKind;
        }

        // Two Pair
        if (valueCounts.Values.Count(count => count == 2) >= 2)
        {
            List<int> pairValues = valueCounts.Where(pair => pair.Value == 2).Select(pair => pair.Key).OrderByDescending(value => value).Take(2).ToList();
            Debug.Log($"2 pair, values:{pairValues[0]}, {pairValues[1]}");
            return PokerHand.TwoPair;
        }

        // One Pair
        if (valueCounts.Values.Any(count => count == 2))
        {
            Debug.Log($"pair, card:{valueCounts.First(pair => pair.Value == 2).Key}");

            return PokerHand.OnePair;
        }

        // High Card
        Debug.Log($"high card:{cards.Max(c => c.cardValue)}");
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
