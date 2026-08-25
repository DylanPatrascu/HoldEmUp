using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

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
        (PokerHand, List<int>) playerScore = EvaluateScore(communityCards, playerHand);
        (PokerHand, List<int>) clubOpponentScore = EvaluateScore(communityCards, clubOpponentHand);
        (PokerHand, List<int>) heartOpponentScore = EvaluateScore(communityCards, heartOpponentHand);
        (PokerHand, List<int>) spadeOpponentScore = EvaluateScore(communityCards, spadeOpponentHand);
        (PokerHand, List<int>) diamondOpponentScore = EvaluateScore(communityCards, diamondOpponentHand);

        Debug.Log("Player: " + playerScore.Item1 + "Cards: " + string.Join(", ", playerScore.Item2));
        Debug.Log("Club Opponent: " + clubOpponentScore.Item1 + "Cards: " + string.Join(", ", clubOpponentScore.Item2));
        Debug.Log("Heart Opponent: " + heartOpponentScore.Item1 + "Cards: " + string.Join(", ", heartOpponentScore.Item2));
        Debug.Log("Spade Opponent: " + spadeOpponentScore.Item1 + "Cards: " + string.Join(", ", spadeOpponentScore.Item2));
        Debug.Log("Diamond Opponent: " + diamondOpponentScore.Item1 + "Cards: " + string.Join(", ", diamondOpponentScore.Item2));

    }

    public (PokerHand, List<int>) EvaluateScore(List<PlayingCard> communityCards, List<PlayingCard> hand)
    {
        List<PlayingCard> usableCards = new List<PlayingCard>(communityCards);
        // Add hand to the community cards for the total 7 usable cards
        usableCards.AddRange(hand);

        PokerHand bestHand = PokerHand.Empty;
        List<int> highCardValue = new List<int>();

        var uniqueHands = GetCombinations(usableCards, 5).ToList();
        
        for (int i = 0; i < uniqueHands.Count; i++)
        {
            PokerHand currentHand = EvaluateFiveCards(uniqueHands[i].ToList());
            if (currentHand == bestHand)
            {
                Debug.Log("current hand being checked is" + currentHand);
            }
            // higher enum value = stronger hand
            if (currentHand > bestHand)
            {
                //Debug.Log($"NEW BEST: {currentHand} | " + $"{string.Join(", ", uniqueHands[i].ToList())}");
                bestHand = currentHand;

                List<PlayingCard> bestHandCards = uniqueHands[i].ToList().OrderByDescending(c => c.cardValue).ToList();
                Dictionary<int, int> bestHandValues = bestHandCards.GroupBy(c => c.cardValue).ToDictionary(group => group.Key, group => group.Count());
                highCardValue.Clear();


                switch (bestHand)
                {
                    case PokerHand.RoyalFlush:
                        highCardValue.Add(bestHandCards.Max(c => c.cardValue));
                        break;
                    case PokerHand.StraightFlush:
                        highCardValue.Add(bestHandCards.Max(c => c.cardValue));
                        break;
                    case PokerHand.FourOfAKind:
                        highCardValue.Add(bestHandValues.First(pair => pair.Value == 4).Key);
                        break;
                    case PokerHand.FullHouse:
                        highCardValue.Add(bestHandValues.First(pair => pair.Value == 3).Key); // index 0 is always the triple
                        highCardValue.Add(bestHandValues.First(pair => pair.Value == 2).Key);
                        break;
                    case PokerHand.Flush:
                        highCardValue.Add(bestHandCards.Max(c => c.cardValue));
                        break;
                    case PokerHand.Straight:
                        highCardValue.Add(bestHandCards.Max(c => c.cardValue));
                        break;
                    case PokerHand.ThreeOfAKind:
                        highCardValue.Add(bestHandValues.First(pair => pair.Value == 3).Key);
                        break;
                    case PokerHand.TwoPair:
                        highCardValue = bestHandValues.Where(pair => pair.Value == 2).Select(pair => pair.Key).OrderByDescending(value => value).Take(2).ToList();
                        break;
                    case PokerHand.OnePair:
                        highCardValue.Add(bestHandValues.First(pair => pair.Value == 2).Key);
                        break;
                    case PokerHand.HighCard:
                        Debug.Log("entered");
                        highCardValue.Add(bestHandCards.Max(c => c.cardValue));
                        break;
                    default: // High Card
                        highCardValue.Add(bestHandCards.Max(c => c.cardValue));
                        break;
                }
            }
        }

        return (bestHand, highCardValue);
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
