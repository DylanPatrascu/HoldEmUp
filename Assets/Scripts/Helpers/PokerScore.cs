using System.Collections.Generic;

public class PokerScore
{
    public PokerHand pokerHand;
    public List<int> pokerCards;

    public PokerScore(PokerHand hand, List<int> cards)
    {
        pokerHand = hand;
        pokerCards = cards;
    }

    public PokerScore()
    {
        pokerHand = PokerHand.Empty;
        pokerCards = new List<int>();
    }
}
