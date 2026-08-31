using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
public class PokerVisualManager : MonoBehaviour
{
    public static PokerVisualManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject playingCardPrefab;
    [SerializeField] private GameObject tablePlayingCardPrefab;

    [Header("Hands and Table")]
    [SerializeField] private Transform cardDeckTransform;
    [SerializeField] private Transform playerHandTransform;
    [SerializeField] private Transform clubOpponentHandTransform;
    [SerializeField] private Transform spadeOpponentHandTransform;
    [SerializeField] private Transform heartOpponentHandTransform;
    [SerializeField] private Transform diamondOpponentHandTransform;
    [SerializeField] private Transform tableCardTransform;

    [Header("Revealed Hands")]
    [SerializeField] private Transform clubRevealedTransform;
    [SerializeField] private Transform spadeRevealedTransform;
    [SerializeField] private Transform heartRevealedTransform;
    [SerializeField] private Transform diamondRevealedTransform;

    [Header("Current Bets")]
    [SerializeField] private GameObject spadesBets;
    [SerializeField] private TMP_Text spadesCurrentBetText;
    [SerializeField] private GameObject diamondsBets;
    [SerializeField] private TMP_Text diamondsCurrentBetText;
    [SerializeField] private GameObject heartsBets;
    [SerializeField] private TMP_Text heartsCurrentBetText;
    [SerializeField] private GameObject clubsBets;
    [SerializeField] private TMP_Text clubsCurrentBetText;

    private const float CARD_MOVEMENT_SPEED = 0.67f;
    private const float CARD_ORITENTATION_SPEED = 0.5f;
    private Vector3 CARD_OFFSET = new Vector3(0.7f, 0.1f, 0);

    [SerializeField] private TMP_Text sentenceText;
    [SerializeField] private float textSpeed;

    private Queue<string> messageQueue = new Queue<string>();
        private bool isDisplayingMessage = false;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        PokerGameManager.Instance.PerformedPlayerAction += UpdateCurrentBets;
    }

    private List<KeyValuePair<PokerPosition, List<PlayingCard>>> GetHandsFlattened()
    {
        Dictionary<PokerPosition, List<PlayingCard>> hands = new Dictionary<PokerPosition,List<PlayingCard>>(PokerManager.Instance.allH);

        List <KeyValuePair<PokerPosition, List<PlayingCard>>> allHandsFlattened = hands.ToList();
        return allHandsFlattened.OrderBy(c => (int)c.Key).ToList();
    }

    public IEnumerator DealToCommunityCards()
    {
        List<PlayingCard> communityCards = new List<PlayingCard>(PokerManager.Instance.communityCards);
        if (communityCards.Count == 3)
        {
            for (int i = 0; i < communityCards.Count; i++)
            {
                yield return StartCoroutine(SpawnCard(communityCards[i], PokerPosition.Table));
            }
        }
        else if (communityCards.Count == 4)
        {
            yield return StartCoroutine(SpawnCard(communityCards[3], PokerPosition.Table));
        }
        else if (communityCards.Count == 5)
        {
            yield return StartCoroutine(SpawnCard(communityCards[4], PokerPosition.Table));
        }
        yield return null;
    }
    public IEnumerator DealCardsToEveryone()
    {
        PokerPosition smallBlind = PokerGameManager.Instance.SmallBlind;

        List<KeyValuePair<PokerPosition, List<PlayingCard>>> flattenedHands = GetHandsFlattened();

        int startIndex = flattenedHands.FindIndex(x => x.Key == smallBlind);

        if (startIndex == -1)
        {
            Debug.LogError($"Could not find Small Blind {smallBlind} in flattenedHands.");
            yield break;
        }

        // First card
        for (int i = 0; i < flattenedHands.Count; i++)
        {
            int index = (startIndex + i) % flattenedHands.Count;
            var curr = flattenedHands[index];

            yield return StartCoroutine(SpawnCard(curr.Value[0], curr.Key));
        }

        // Second card
        for (int i = 0; i < flattenedHands.Count; i++)
        {
            int index = (startIndex + i) % flattenedHands.Count;
            var curr = flattenedHands[index];

            yield return StartCoroutine(SpawnCard(curr.Value[1], curr.Key));
        }

        OffsetCardsInHands();
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
                Vector3 position = tableCardTransform.position + new Vector3(tableCardTransform.childCount * 0.8f, 0, 0);
                yield return cardObject.transform.DOMove(position, CARD_MOVEMENT_SPEED).WaitForCompletion();
                cardObject.transform.SetParent(tableCardTransform);
                yield return cardObject.transform.DORotate(new Vector3(0, 180, 0), 0.3f);
                visualCard.ShowCardData();

                //cardObject = Instantiate(tablePlayingCardPrefab, tableCardTransform);
                //cardObject.transform.localPosition += new Vector3((tableCardTransform.transform.childCount - 1) * 1.3f, 0, 0); // To offset the cards on the table
                break;
        }

        yield return null;
    }

    public void RevealHands()
    {
        for (int i = 0; i <= 1;  i++)
        {
            Transform clubChild = clubOpponentHandTransform.GetChild(0);
            Transform heartChild = heartOpponentHandTransform.GetChild(0);
            Transform diamondChild = diamondOpponentHandTransform.GetChild(0);
            Transform spadeChild = spadeOpponentHandTransform.GetChild(0);

            clubChild.gameObject.GetComponent<VisualPlayingCard>().ShowCardData();
            clubChild.SetParent(clubRevealedTransform);
            clubChild.DOLocalMove(Vector3.zero, CARD_MOVEMENT_SPEED);
            clubChild.DOLocalRotate(Vector3.zero, CARD_MOVEMENT_SPEED);

            //clubChild.DOLocalRotate(new Vector3(90, 0, 0), CARD_ORITENTATION_SPEED);
            heartChild.gameObject.GetComponent<VisualPlayingCard>().ShowCardData();
            heartChild.SetParent(heartRevealedTransform);
            heartChild.DOLocalMove(Vector3.zero, CARD_MOVEMENT_SPEED);
            heartChild.DOLocalRotate(Vector3.zero, CARD_MOVEMENT_SPEED);

            //heartChild.DOLocalRotate(new Vector3(90, 0, 0), CARD_ORITENTATION_SPEED);
            diamondChild.gameObject.GetComponent<VisualPlayingCard>().ShowCardData();
            diamondChild.SetParent(diamondRevealedTransform);
            diamondChild.DOLocalMove(Vector3.zero, CARD_MOVEMENT_SPEED);
            diamondChild.DOLocalRotate(Vector3.zero, CARD_MOVEMENT_SPEED);

            //diamondChild.DOLocalRotate(new Vector3(90, 0, 0), CARD_ORITENTATION_SPEED);
            spadeChild.gameObject.GetComponent<VisualPlayingCard>().ShowCardData();
            spadeChild.SetParent(spadeRevealedTransform);
            spadeChild.DOLocalMove(Vector3.zero, CARD_MOVEMENT_SPEED);
            spadeChild.DOLocalRotate(Vector3.zero, CARD_MOVEMENT_SPEED);

            //spadeChild.DOLocalRotate(new Vector3(90, 0, 0), CARD_ORITENTATION_SPEED);
            Vector3 position = clubChild.position + new Vector3(clubOpponentHandTransform.childCount * 0.8f, 0, 0);
            clubChild.DOLocalMoveX(-0.5f * (clubOpponentHandTransform.childCount - 1), CARD_MOVEMENT_SPEED);
            
            position = heartChild.position + new Vector3(heartRevealedTransform.childCount * 0.8f, 0, 0);
            heartChild.DOLocalMoveX(-0.5f * (heartRevealedTransform.childCount - 1), CARD_MOVEMENT_SPEED);
            
            position = diamondChild.position + new Vector3(diamondRevealedTransform.childCount * 0.8f, 0, 0);
            diamondChild.DOLocalMoveX(-0.5f * (diamondRevealedTransform.childCount - 1), CARD_MOVEMENT_SPEED);

            position = spadeChild.position + new Vector3(spadeRevealedTransform.childCount * 0.8f, 0, 0);
            spadeChild.DOLocalMoveX(-0.5f * (spadeRevealedTransform.childCount - 1), CARD_MOVEMENT_SPEED);
            AudioManager.Instance.PlayAudioClip(AudioSnippet.PlayingCardFlip);
        }
    }
    public void OffsetCardsInHands()
    {
        Debug.Log("hwheahew" + playerHandTransform.childCount);
        for (int i = 0; i <= playerHandTransform.childCount - 1; i++)
        {
            playerHandTransform.GetChild(i).DOLocalMove(Vector3.zero + (CARD_OFFSET * i), CARD_ORITENTATION_SPEED);
            playerHandTransform.GetChild(i).DOLocalRotate(Vector3.zero, .5f);
            playerHandTransform.GetChild(i).GetComponent<VisualPlayingCard>().ShowCardData();
            clubOpponentHandTransform.GetChild(i).DOLocalMove(Vector3.zero + (CARD_OFFSET * i), CARD_ORITENTATION_SPEED);
            clubOpponentHandTransform.GetChild(i).DOLocalRotate(Vector3.zero, .5f);
            spadeOpponentHandTransform.GetChild(i).DOLocalMove(Vector3.zero + (CARD_OFFSET * i), CARD_ORITENTATION_SPEED);
            spadeOpponentHandTransform.GetChild(i).DOLocalRotate(Vector3.zero, .5f);
            heartOpponentHandTransform.GetChild(i).DOLocalMove(Vector3.zero + (CARD_OFFSET * i), CARD_ORITENTATION_SPEED);
            heartOpponentHandTransform.GetChild(i).DOLocalRotate(Vector3.zero, .5f);
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

    public void UpdateCurrentBets(object sender, PokerGameManager.PokerEvent e)
    {
        if (e.Action == PokerAction.Fold)
        {
            switch (e.Player)
            {
                case PokerPosition.Heart:
                    heartsBets.SetActive(false);
                    break;
                case PokerPosition.Diamond:
                    diamondsBets.SetActive(false);
                    break;
                case PokerPosition.Spade:
                    spadesBets.SetActive(false);
                    break;
                case PokerPosition.Club:
                    clubsBets.SetActive(false);
                    break;
            }
        }
        else if (e.Action == PokerAction.Call)
        {
            switch (e.Player)
            {
                case PokerPosition.Heart:
                    heartsCurrentBetText.text = BettingManager.Instance.GetHighestBet(PokerGameManager.Instance.ActivePlayers.ToList()).ToString();
                    break;
                case PokerPosition.Diamond:
                    diamondsCurrentBetText.text = BettingManager.Instance.GetHighestBet(PokerGameManager.Instance.ActivePlayers.ToList()).ToString(); ;
                    break;
                case PokerPosition.Spade:
                    spadesCurrentBetText.text = BettingManager.Instance.GetHighestBet(PokerGameManager.Instance.ActivePlayers.ToList()).ToString(); ;
                    break;
                case PokerPosition.Club:
                    clubsCurrentBetText.text = BettingManager.Instance.GetHighestBet(PokerGameManager.Instance.ActivePlayers.ToList()).ToString(); ;
                    break;
            }
        }
        else if (e.Action == PokerAction.Check)
        {
            switch (e.Player)
            {
                case PokerPosition.Heart:
                    heartsCurrentBetText.text = "0";
                    break;
                case PokerPosition.Diamond:
                    diamondsCurrentBetText.text = "0";
                    break;
                case PokerPosition.Spade:
                    spadesCurrentBetText.text = "0";
                    break;
                case PokerPosition.Club:
                    clubsCurrentBetText.text = "0";
                    break;
            }
        }
        else if (e.Action != PokerAction.Fold && e.Action != PokerAction.Check)
        {
            switch (e.Player)
            {
                case PokerPosition.Heart:
                    heartsCurrentBetText.text = e.Amount.ToString();
                    break;
                case PokerPosition.Diamond:
                    diamondsCurrentBetText.text = e.Amount.ToString();
                    break;
                case PokerPosition.Spade:
                    spadesCurrentBetText.text = e.Amount.ToString();
                    break;
                case PokerPosition.Club:
                    clubsCurrentBetText.text = e.Amount.ToString();
                    break;
            }
        }
    }

    #region Event Log
    public void DisplaySentence(string sentence)
    {
        messageQueue.Enqueue(sentence);
        if (!isDisplayingMessage)
        {
            StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        isDisplayingMessage = true;

        while (messageQueue.Count > 0)
        {
            string currentSentence = messageQueue.Dequeue();
            yield return StartCoroutine(RenderSentence(currentSentence));

            // Small pause between messages
            yield return new WaitForSeconds(0.5f);
        }

        isDisplayingMessage = false;
    }

    private IEnumerator RenderSentence(string sentence)
    {
        // Keep existing text, then append the new sentence on a new line.
        string previousText = sentenceText.text;
        if (!string.IsNullOrEmpty(previousText))
            previousText += "\n";

        sentenceText.text = previousText;

        foreach (char letter in sentence)
        {
            sentenceText.text += letter;

            // After adding a letter, check line count
            TrimLines(5);

            yield return new WaitForSeconds(textSpeed);
        }
    }

    private void TrimLines(int maxLines)
    {
        string[] lines = sentenceText.text.Split('\n');
        if (lines.Length > maxLines)
        {
            // Remove oldest line(s)
            int excess = lines.Length - maxLines;
            sentenceText.text = string.Join("\n", lines, excess, lines.Length - excess);
        }
    }
    #endregion


}
