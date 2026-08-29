using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class PokerVisualManager : MonoBehaviour
{
    public static PokerVisualManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject playingCardPrefab;
    [SerializeField] private GameObject tablePlayingCardPrefab;

    [Header("Transforms")]
    [SerializeField] private Transform cardDeckTransform;
    [SerializeField] private Transform playerHandTransform;
    [SerializeField] private Transform clubOpponentHandTransform;
    [SerializeField] private Transform spadeOpponentHandTransform;
    [SerializeField] private Transform heartOpponentHandTransform;
    [SerializeField] private Transform diamondOpponentHandTransform;
    [SerializeField] private Transform tableCardTransform;

    private const float CARD_MOVEMENT_SPEED = 0.67f;
    private const float CARD_ORITENTATION_SPEED = 0.5f;
    private Vector3 CARD_OFFSET = new Vector3(0.7f, 0.1f, 0);


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private List<KeyValuePair<PokerPosition, List<PlayingCard>>> GetHandsFlattened()
    {
        Dictionary<PokerPosition, List<PlayingCard>> hands = new Dictionary<PokerPosition,List<PlayingCard>>(PokerManager.Instance.allH);

        List <KeyValuePair<PokerPosition, List<PlayingCard>>> allHandsFlattened = hands.ToList();
        return allHandsFlattened.OrderBy(c => (int)c.Key).ToList();
    }

    public IEnumerator DealToCommunityCards()
    {
        List<PlayingCard> communityCards;
        yield return null;
    }
    public IEnumerator DealCardsToEveryone()
    {
        PokerPosition player = PokerGameManager.Instance.SmallBlind;
        List<KeyValuePair<PokerPosition, List<PlayingCard>>> flattenedHands = GetHandsFlattened();

        for (int i = (int)player; i < flattenedHands.Count; i++)
        {
            var curr = flattenedHands[i % flattenedHands.Count];
            yield return StartCoroutine(SpawnCard(curr.Value[0], curr.Key));
        }

        for (int i = (int)player; i < flattenedHands.Count; i++)
        {
            var curr = flattenedHands[i % flattenedHands.Count];
            yield return StartCoroutine(SpawnCard(curr.Value[1], curr.Key));
        }

        OffsetCardsInHands();
        yield return null;
    }
    public IEnumerator SpawnCard(PlayingCard card, PokerPosition player)
    {
        GameObject cardObject = Instantiate(playingCardPrefab, cardDeckTransform);
        VisualPlayingCard visualCard = cardObject.GetComponent<VisualPlayingCard>();
        if (player != PokerPosition.Table)
        {
            cardObject.transform.rotation *= Quaternion.Euler(0, 180f, 0);
        }
        visualCard.PopulateData(card);
        visualCard.HideCardData();
        AudioManager.Instance.PlayAudioClip(AudioSnippet.PlayingCardDeal);

        switch (player)
        {
            case PokerPosition.Joker:
            default:
                //cardObject = Instantiate(playingCardPrefab, playerHandTransform);
                yield return cardObject.transform.DOMove(playerHandTransform.position, CARD_MOVEMENT_SPEED).WaitForCompletion();
                cardObject.transform.SetParent(playerHandTransform);
                break;

            case PokerPosition.Heart:
                //cardObject = Instantiate(playingCardPrefab, heartOpponentHandTransform);
                yield return cardObject.transform.DOMove(heartOpponentHandTransform.position, CARD_MOVEMENT_SPEED).WaitForCompletion();
                cardObject.transform.SetParent(heartOpponentHandTransform);
                break;

            case PokerPosition.Spade:
                //cardObject = Instantiate(playingCardPrefab, spadeOpponentHandTransform);
                yield return cardObject.transform.DOMove(spadeOpponentHandTransform.position, CARD_MOVEMENT_SPEED).WaitForCompletion();
                cardObject.transform.SetParent(spadeOpponentHandTransform);
                break;

            case PokerPosition.Club:
                //cardObject = Instantiate(playingCardPrefab, clubOpponentHandTransform);
                yield return cardObject.transform.DOMove(clubOpponentHandTransform.position, CARD_MOVEMENT_SPEED).WaitForCompletion();
                cardObject.transform.SetParent(clubOpponentHandTransform);
                break;

            case PokerPosition.Diamond:
                //cardObject = Instantiate(playingCardPrefab, diamondOpponentHandTransform);
                yield return cardObject.transform.DOMove(diamondOpponentHandTransform.position, CARD_MOVEMENT_SPEED).WaitForCompletion();
                cardObject.transform.SetParent(diamondOpponentHandTransform);
                break;

            case PokerPosition.Table:
                yield return cardObject.transform.DOMove(tableCardTransform.position += new Vector3((tableCardTransform.transform.childCount - 1) * 1.1f, 0, 0), CARD_MOVEMENT_SPEED).WaitForCompletion();
                cardObject.transform.SetParent(tableCardTransform);

                //cardObject = Instantiate(tablePlayingCardPrefab, tableCardTransform);
                //cardObject.transform.localPosition += new Vector3((tableCardTransform.transform.childCount - 1) * 1.3f, 0, 0); // To offset the cards on the table
                break;
        }

        OffsetCardsInHands();
        yield return null;
    }

    public void OffsetCardsInHands()
    {
        for (int i = 0; i <= 1; i++)
        {
            playerHandTransform.GetChild(i).DOLocalMove(Vector3.zero + (CARD_OFFSET * i), CARD_ORITENTATION_SPEED);
            playerHandTransform.GetChild(i).DOLocalRotate(Vector3.zero, .5f);
            playerHandTransform.GetChild(i).GetComponent<VisualPlayingCard>().ShowCardData();

        }
        for (int i = 0; i <= 1; i++)
        {
            clubOpponentHandTransform.GetChild(i).DOLocalMove(Vector3.zero + (CARD_OFFSET * i), CARD_ORITENTATION_SPEED);
            clubOpponentHandTransform.GetChild(i).DOLocalRotate(Vector3.zero, .5f);
        }
        for (int i = 0; i <= 1; i++)
        {
            spadeOpponentHandTransform.GetChild(i).DOLocalMove(Vector3.zero + (CARD_OFFSET * i), CARD_ORITENTATION_SPEED);
            spadeOpponentHandTransform.GetChild(i).DOLocalRotate(Vector3.zero, .5f);
        }
        for (int i = 0; i <= 1; i++)
        {
            heartOpponentHandTransform.GetChild(i).DOLocalMove(Vector3.zero + (CARD_OFFSET * i), CARD_ORITENTATION_SPEED);
            heartOpponentHandTransform.GetChild(i).DOLocalRotate(Vector3.zero, .5f);
        }
        for (int i = 0; i <= 1; i++)
        {
            diamondOpponentHandTransform.GetChild(i).DOLocalMove(Vector3.zero + (CARD_OFFSET * i), CARD_ORITENTATION_SPEED);
            diamondOpponentHandTransform.GetChild(i).DOLocalRotate(Vector3.zero, .5f);
        }
    }

    // Will get replaced with cards being given back to dealer probabky
    public void DestroyCardVisuals()
    {
        // Move them off of a their respective gameobjects, because otherwise it needs to wait until end of frame
        for (int i = tableCardTransform.transform.childCount - 1; i >= 0; i--)
        {
            //tableCardTransform.transform.DOMove(cardDeckTransform.transform.position, CARD_MOVEMENT_SPEED);
            tableCardTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = playerHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            //playerHandTransform.transform.DOMove(cardDeckTransform.transform.position, CARD_MOVEMENT_SPEED);
            playerHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = clubOpponentHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            //clubOpponentHandTransform.transform.DOMove(cardDeckTransform.transform.position, CARD_MOVEMENT_SPEED);
            clubOpponentHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = spadeOpponentHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            //spadeOpponentHandTransform.transform.DOMove(cardDeckTransform.transform.position, CARD_MOVEMENT_SPEED);
            spadeOpponentHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = heartOpponentHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            //heartOpponentHandTransform.transform.DOMove(cardDeckTransform.transform.position, CARD_MOVEMENT_SPEED);
            heartOpponentHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = diamondOpponentHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            //diamondOpponentHandTransform.transform.DOMove(cardDeckTransform.transform.position, CARD_MOVEMENT_SPEED);
            diamondOpponentHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

}
