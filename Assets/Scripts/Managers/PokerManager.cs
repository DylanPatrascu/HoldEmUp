using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PokerManager : MonoBehaviour
{
    public static PokerManager Instance { get; private set; }

    [Header("Hands")]
    [SerializeField] private List<PlayingCard> jokerHand;
    [SerializeField] private List<PlayingCard> clubOpponentHand;
    [SerializeField] private List<PlayingCard> spadeOpponentHand;
    [SerializeField] private List<PlayingCard> heartOpponentHand;
    [SerializeField] private List<PlayingCard> diamondOpponentHand;

    public List<PlayingCard> communityCards;
    private List<PlayingCard> deckList;
    private List<PlayingCard> runtimeDeck;

    public Dictionary<PokerPosition, List<PlayingCard>> allH;
    public List<PlayingCard> GetHand(PokerPosition player) => allH[player];

    #region Unity Methods
    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        deckList = Resources.LoadAll<PlayingCard>("PlayingCards").ToList();
        runtimeDeck = new List<PlayingCard>(deckList);
        allH = new Dictionary<PokerPosition, List<PlayingCard>>
        {
            [PokerPosition.Joker] = jokerHand,
            [PokerPosition.Club] = clubOpponentHand,
            [PokerPosition.Spade] = spadeOpponentHand,
            [PokerPosition.Diamond] = diamondOpponentHand,
            [PokerPosition.Heart] = heartOpponentHand,
        };
    }
    public void Start()
    {
        //ResetGame();
    }

    #endregion

    #region Poker Game Methods
    public void DealCards()
    {
        foreach (KeyValuePair<PokerPosition, List<PlayingCard>> entry in allH)
        {
            PokerPosition position = entry.Key;
            List<PlayingCard> hand = entry.Value;
            DrawCard(hand, position);
        }

        foreach (KeyValuePair<PokerPosition, List<PlayingCard>> entry in allH)
        {
            PokerPosition position = entry.Key;
            List<PlayingCard> hand = entry.Value;
            DrawCard(hand, position);
            Debug.Log(position);

        }


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

    public void BurnCard(int numCards = 1)
    {
        runtimeDeck.Remove(runtimeDeck[0]);
    }

    public void DestroyCards()
    {
        foreach (KeyValuePair<PokerPosition, List<PlayingCard>> entry in allH)
        {
            entry.Value.Clear();
        }
        communityCards.Clear();
        runtimeDeck.Clear();

        // Fresh playing deck
        runtimeDeck = new List<PlayingCard>(deckList);
        HelperMethods.Shuffle(runtimeDeck);

        PokerVisualManager.Instance.DestroyCardVisuals();
    }
    /*
     public void ResetGame()
    {
        DestroyCards();

        DealCards();

        PokerVisualManager.Instance.OffsetCardsInHands();
        
        //small blind, big blind -> start on player and rotate clockwise. 1$ and 2$
        
        //Pre-Flop
        // each player folds, calls or raises the big blind. starts left of big blind
        // betting continues until each active player have bet equal bets in the pot

        //Flop
        BurnCard();
        DrawCard(communityCards, PokerPosition.Table, 3);
        //betting starts at the person with the button (left of dealer)
        //same options as above, but if no one has bet yet, you can check
        
        //Turn
        BurnCard();
        DrawCard(communityCards, PokerPosition.Table, 1);
        //betting starts at the person with the button (left of dealer)
        //same options as above, but if no one has bet yet, you can check

        //River
        BurnCard();
        DrawCard(communityCards, PokerPosition.Table, 1);
        //betting starts at the person with the button (left of dealer)
        //same options as above, but if no one has bet yet, you can check

        //reveal hands starting left of dealer
        //if draw, divide pot equally
    }

    */

    public void CheckWin()
    {
        List<PokerScore> allScores = new List<PokerScore>();

        foreach (KeyValuePair<PokerPosition, List<PlayingCard>> entry in allH)
        {
            allScores.Add(EvaluateScore(communityCards, entry.Value));

        }

        // Start by assuming player has the best hand
        PokerScore winningScore = allScores[0];

        // Check everyone else
        for (int i = 1; i < allScores.Count; i++)
        {
            if (IsBetterScore(allScores[i], winningScore))
            {
                winningScore = allScores[i];
            }
        }

        //find anyone with better score

        List<int> winnerIndexes = new List<int>();

        for (int i = 0; i < allScores.Count; i++)
        {
            if (IsSameScore(allScores[i], winningScore))
            {
                winnerIndexes.Add(i);
            }
        }


        //output
        if (winnerIndexes.Count == 1)
        {
            // ONE winner
            Debug.Log($"WINNER NAME: {(PokerPosition)winnerIndexes[0]} | " + $"HAND: {winningScore.pokerHand} | " + $"SCORE: {string.Join(", ", winningScore.pokerCards)}");
        }
        else
        {
            // tie
            Debug.Log($"TIE! HAND: {winningScore.pokerHand} | " + $"SCORE: {string.Join(", ", winningScore.pokerCards)}");
            foreach (int i in winnerIndexes)
            {
                Debug.Log($"{(PokerPosition)i}");
            }
        }
    }

    #endregion

    #region Poker Comparisons

    public bool IsFlush(List<PlayingCard> cards)
    {
        return cards.All(card => card.cardSuit == cards[0].cardSuit);
    }

    public bool IsStraight(List<PlayingCard> cards)
    {
        List<int> values = cards.Select(card => card.cardValue).Distinct().OrderByDescending(value => value).ToList();

        // A straight must contain 5 unique values
        if (values.Count != 5)
        {
            return false;
        }

        // Normal straight
        if (values[0] - values[4] == 4)
        {
            return true;
        }

        // Low straight
        if (values.SequenceEqual(new List<int> { 14, 5, 4, 3, 2 }))
        {
            return true;
        }

        return false;
    }

    public bool IsSameScore(PokerScore score1, PokerScore score2)
    {
        // Different hand types = not a tie
        if (score1.pokerHand != score2.pokerHand)
        {
            return false;
        }

        // Different number of tiebreaker values = not a tie
        if (score1.pokerCards.Count != score2.pokerCards.Count)
        {
            return false;
        }

        // Compare every tiebreaker value
        for (int i = 0; i < score1.pokerCards.Count; i++)
        {
            if (score1.pokerCards[i] != score2.pokerCards[i])
            {
                return false;
            }
        }

        // Same hand type AND same tiebreaker values
        return true;
    }
    public bool IsBetterScore(PokerScore score1, PokerScore score2)
    {
        if (score1.pokerHand > score2.pokerHand)
        {
            return true;
        }

        if (score1.pokerHand < score2.pokerHand)
        {
            return false;
        }
        
        // If the hands are the same, compare the cards themselves
        return HelperMethods.CompareValues(score1.pokerCards, score2.pokerCards) > 0;
    }

    // Determines the best poker hand of 7 cards
    public PokerScore EvaluateScore(List<PlayingCard> communityCards, List<PlayingCard> hand)
    {
        List<PlayingCard> usableCards = new List<PlayingCard>(communityCards);
        usableCards.AddRange(hand);

        PokerScore bestPokerScore = new PokerScore();

        IEnumerable<IEnumerable<PlayingCard>> combinations = HelperMethods.GetCombinations(usableCards, 5);

        foreach (IEnumerable<PlayingCard> combination in combinations)
        {
            List<PlayingCard> cards = combination.ToList();
            PokerHand currentHand = EvaluateFiveCards(cards);

            PokerScore currentPokerScore = new PokerScore(currentHand, GetScoreValues(currentHand, cards));

            // Better poker hand
            if (currentPokerScore.pokerHand > bestPokerScore.pokerHand)
            {
                bestPokerScore.pokerHand = currentPokerScore.pokerHand;
                bestPokerScore.pokerCards = currentPokerScore.pokerCards;
            }
            // Same poker hand -> compare tiebreakers
            else if (currentPokerScore.pokerHand == bestPokerScore.pokerHand && HelperMethods.CompareValues(currentPokerScore.pokerCards, bestPokerScore.pokerCards) > 0)
            {
                bestPokerScore.pokerCards = currentPokerScore.pokerCards;
            }
        }

        return bestPokerScore;
    }

    // Determines what kind of poker hand 5 cards are
    public PokerHand EvaluateFiveCards(List<PlayingCard> cards)
    {
        cards = cards.OrderByDescending(card => card.cardValue).ToList();

        Dictionary<int, int> valueCounts = cards.GroupBy(card => card.cardValue).ToDictionary(group => group.Key, group => group.Count());

        bool isFlush = IsFlush(cards);
        bool isStraight = IsStraight(cards);

        // Royal Flush
        if (isFlush && isStraight && cards[0].cardValue == 14 && cards[1].cardValue == 13)
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
    #endregion

    #region Poker Getters
    // Gets list of required cards to evaluate a poker hand
    public List<int> GetScoreValues(PokerHand handType, List<PlayingCard> cards)
    {
        Dictionary<int, int> valueCounts = cards.GroupBy(card => card.cardValue).ToDictionary(group => group.Key, group => group.Count());

        switch (handType)
        {
            case PokerHand.RoyalFlush:
                return new List<int> { 14 };

            case PokerHand.StraightFlush:
            case PokerHand.Straight:
                {
                    int highCard = GetStraightHighCard(cards);

                    return new List<int> { highCard };
                }

            case PokerHand.FourOfAKind:
                {
                    int fourOfAKind = valueCounts.First(pair => pair.Value == 4).Key;

                    int tiebreaker = valueCounts.Where(pair => pair.Value != 4).Max(pair => pair.Key);

                    return new List<int> { fourOfAKind, tiebreaker };
                }

            case PokerHand.FullHouse:
                {
                    int triple = valueCounts.Where(pair => pair.Value == 3).Max(pair => pair.Key);
                    int pair = valueCounts.Where(pair => pair.Value == 2).Max(pair => pair.Key);
                    return new List<int> { triple, pair };
                }

            case PokerHand.Flush:
                {
                    return cards.Select(card => card.cardValue).OrderByDescending(value => value).ToList();
                }
            case PokerHand.ThreeOfAKind:
                {
                    int triple = valueCounts.First(pair => pair.Value == 3).Key;

                    List<int> tiebreakers = cards.Where(card => card.cardValue != triple).Select(card => card.cardValue).OrderByDescending(value => value).ToList();

                    return new List<int> { triple }.Concat(tiebreakers).ToList();
                }
            case PokerHand.TwoPair:
                {
                    List<int> pairs = valueCounts.Where(pair => pair.Value == 2).Select(pair => pair.Key).OrderByDescending(value => value).ToList();

                    int tiebreaker = valueCounts.Where(pair => pair.Value == 1).Max(pair => pair.Key);

                    return new List<int> { pairs[0], pairs[1], tiebreaker };
                }
            case PokerHand.OnePair:
                {
                    int pair = valueCounts.First(pair => pair.Value == 2).Key;

                    List<int> tiebreakers = cards.Where(card => card.cardValue != pair).Select(card => card.cardValue).OrderByDescending(value => value).ToList();

                    return new List<int> { pair }.Concat(tiebreakers).ToList();
                }
            case PokerHand.HighCard:
            default:
                {
                    return cards.Select(card => card.cardValue).OrderByDescending(value => value).ToList();
                }
        }
    }

    // Gets the highest card in a Straight
    public int GetStraightHighCard(List<PlayingCard> cards)
    {
        List<int> values = cards.Select(card => card.cardValue).Distinct().OrderByDescending(value => value).ToList();

        //if ace is included, it is technically 1
        if (values.SequenceEqual(new List<int> { 14, 5, 4, 3, 2 }))
        {
            return 5;
        }

        return values.Max();
    }
    #endregion
}
