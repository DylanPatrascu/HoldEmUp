using UnityEngine;
public class PokerVisualManager : MonoBehaviour
{
    public static PokerVisualManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject playingCardPrefab;
    [SerializeField] private GameObject tablePlayingCardPrefab;

    [Header("Transforms")]
    [SerializeField] private Transform playerHandTransform;
    [SerializeField] private Transform clubOpponentHandTransform;
    [SerializeField] private Transform spadeOpponentHandTransform;
    [SerializeField] private Transform heartOpponentHandTransform;
    [SerializeField] private Transform diamondOpponentHandTransform;
    [SerializeField] private Transform tableCardTransform;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SpawnCard(PlayingCard card, PokerPosition player)
    {
        GameObject cardObject;
        switch (player)
        {
            default:
                cardObject = Instantiate(playingCardPrefab, playerHandTransform);
                break;
            case PokerPosition.Heart:
                cardObject = Instantiate(playingCardPrefab, heartOpponentHandTransform);
                break;
            case PokerPosition.Spade:
                cardObject = Instantiate(playingCardPrefab, spadeOpponentHandTransform);
                break;
            case PokerPosition.Club:
                cardObject = Instantiate(playingCardPrefab, clubOpponentHandTransform);
                break;
            case PokerPosition.Diamond:
                cardObject = Instantiate(playingCardPrefab, diamondOpponentHandTransform);
                break;
            case PokerPosition.Table:
                cardObject = Instantiate(tablePlayingCardPrefab, tableCardTransform);
                cardObject.transform.localPosition += new Vector3((tableCardTransform.transform.childCount - 1) * 1, 0, 0); // To offset the cards on the table
                break;
        }
        VisualPlayingCard visualCard = cardObject.GetComponent<VisualPlayingCard>();
        visualCard.PopulateData(card);
    }

    public void OffsetCardsInHands()
    {
        playerHandTransform.transform.GetChild(0).localPosition += new Vector3(-0.5f, 0, 0);
        playerHandTransform.transform.GetChild(1).localPosition += new Vector3(0.5f, 0, 0);

        clubOpponentHandTransform.transform.GetChild(0).localPosition += new Vector3(-0.5f, 0, 0);
        clubOpponentHandTransform.transform.GetChild(1).localPosition += new Vector3(0.5f, 0, 0);

        spadeOpponentHandTransform.transform.GetChild(0).localPosition += new Vector3(-0.5f, 0, 0);
        spadeOpponentHandTransform.transform.GetChild(1).localPosition += new Vector3(0.5f, 0, 0);

        heartOpponentHandTransform.transform.GetChild(0).localPosition += new Vector3(-0.5f, 0, 0);
        heartOpponentHandTransform.transform.GetChild(1).localPosition += new Vector3(0.5f, 0, 0);

        diamondOpponentHandTransform.transform.GetChild(0).localPosition += new Vector3(-0.5f, 0, 0);
        diamondOpponentHandTransform.transform.GetChild(1).localPosition += new Vector3(0.5f, 0, 0);
    }

    // Will get replaced with cards being given back to dealer probabky
    public void DestroyCardVisuals()
    {
        // Move them off of a their respective gameobjects, because otherwise it needs to wait until end of frame
        for (int i = playerHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            playerHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = clubOpponentHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            clubOpponentHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = spadeOpponentHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            spadeOpponentHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = heartOpponentHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            heartOpponentHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = diamondOpponentHandTransform.transform.childCount - 1; i >= 0; i--)
        {
            diamondOpponentHandTransform.transform.GetChild(i).SetParent(transform);
        }
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

}
