using UnityEngine;

[CreateAssetMenu(fileName = "PlayingCard", menuName = "Scriptable Objects/PlayingCard")]
public class PlayingCard : ScriptableObject
{
    public int cardValue;
    public CardSuit cardSuit;
    public CardColor cardColor;
    public Sprite cardFrontSprite;
    public Sprite cardBackSprite;
}
