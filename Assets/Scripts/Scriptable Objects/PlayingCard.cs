using UnityEngine;

[CreateAssetMenu(fileName = "PlayingCard", menuName = "Scriptable Objects/PlayingCard")]
public class PlayingCard : ScriptableObject
{
    public int cardValue;
    public CardSuit cardSuit;
    public Texture cardFrontSprite;
    public Texture cardBackSprite;

    public override string ToString()
    {
        return $"{cardValue} of {cardSuit}";
    }
}
