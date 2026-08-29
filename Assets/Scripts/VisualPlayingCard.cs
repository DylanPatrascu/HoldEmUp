using System;
using UnityEngine;

public class VisualPlayingCard : MonoBehaviour
{
    [SerializeField] private PlayingCard cardData;
    [SerializeField] private MeshRenderer cardFront;
    [SerializeField] private MeshRenderer cardBack;

    private Material cardFrontMaterial;
    private Material cardBackMaterial;

    public void PopulateData(PlayingCard card)
    {
        cardData = card;
        cardFrontMaterial = new Material(cardFront.material);
        cardBackMaterial = new Material(cardBack.material);

        cardFrontMaterial.mainTexture = card.cardFrontSprite;
        cardBackMaterial.mainTexture = card.cardBackSprite;

        cardFront.material = cardFrontMaterial;
        cardBack.material = cardBackMaterial;
    }

    public void HideCardData()
    {
        cardFrontMaterial.mainTexture = cardData.cardBackSprite;
    }

    public void ShowCardData()
    {
        cardFrontMaterial.mainTexture = cardData.cardFrontSprite;
    }
}
