using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;
    public PlayerActionMode actionMode { get; private set; } = PlayerActionMode.Waiting;

    [Serializable]
    public class NPCPersonality
    {
        public string personalityName = "Default";

        [Range(0f, 1f)] public float aggression = 0.5f; // Do they bet and raise more often?
        [Range(0f, 1f)] public float tightness = 0.5f; // Minimum hand strength to stay in
        [Range(0f, 1f)] public float bluffFrequency = 0.1f; // Acting like they have a hand they don't (Influences actions but can be used for visuals too I guess)
        [Range(0f, 1f)] public float callStation = 0.3f; // Willingness to call with a weak hand (saving from folds)
    }

    [Serializable]
    public struct PositionPersonality
    {
        public PokerPosition position;
        public NPCPersonality personality;
    }

    [Header("Personality Configuration")]
    [SerializeField]
    private List<PositionPersonality> personalityAssignments = new List<PositionPersonality>();

    private Dictionary<PokerPosition, NPCPersonality> personalityMap; // Built based on editor input from serialized list
    private static readonly NPCPersonality DefaultPersonality = new NPCPersonality();

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        PokerGameManager.Instance.GameStateChanged += SetNPCAction;

        BuildPersonalityMap();
    }

    public void BuildPersonalityMap()
    {
        personalityMap = new Dictionary<PokerPosition, NPCPersonality>();
        foreach (PositionPersonality entry in personalityAssignments)
        {
            personalityMap[entry.position] = entry.personality ?? DefaultPersonality;
        }
    }

    public void SetPersonality(PokerPosition position, NPCPersonality personality)
    {
        if (personalityMap == null) BuildPersonalityMap();
        personalityMap[position] = personality ?? DefaultPersonality;
    }

    public NPCPersonality GetPersonality(PokerPosition position)
    {
        if (personalityMap == null) BuildPersonalityMap();
        if (personalityMap[position] == null) return DefaultPersonality;
        return personalityMap[position];
    }

    public void SetNPCAction(object sender, EventArgs e)
    {
        switch (PokerGameManager.Instance.CurrentGameState)
        {
            case PokerGameManager.GameState.Preflop:
                actionMode = PlayerActionMode.PreflopBetting;
                break;
            case PokerGameManager.GameState.Postflop:
                actionMode = PlayerActionMode.Betting;
                break;
            default:
                actionMode = PlayerActionMode.Waiting;
                break;
        }
    }

    // Decision Making

    public PokerAction GetAction(List<PlayingCard> communityCards, List<PlayingCard> hand, PokerPosition player)
    {
        NPCPersonality profile = GetPersonality(player);
        float handStrength = EvaluateHandStrength(communityCards, hand);

        return DecideAction(handStrength, profile);
    }

    // smart randomizer
    private PokerAction DecideAction(float handStrength, NPCPersonality personality)
    {
        bool isBluffing = UnityEngine.Random.value < personality.bluffFrequency;
        float effectiveStrength = isBluffing ? 1f - handStrength : handStrength;
 
        // need stronger hand to stay in
        // loosest personality will still want to fold at the weakest hand (less than 0.15 strength)
        float foldThreshold = 0.15f + personality.tightness * 0.35f; 
        // lower bar to raise or bet
        // aggressive personalities will raise with anything above 0.55 strength, while 0 aggression would need crazy good hand before raising
        float raiseThreshold = 0.55f + (1f - personality.aggression) * 0.35f; 
 
        if (effectiveStrength < foldThreshold && !isBluffing)
        {
            // some personalities will still call regardless of their weak hand
            return UnityEngine.Random.value < personality.callStation
                ? PokerAction.Call
                : PokerAction.Fold;
        }
 
        if (effectiveStrength >= raiseThreshold || isBluffing)
        {
            // 
            return UnityEngine.Random.value < personality.aggression
                ? PokerAction.Raise
                : PokerAction.Bet;
        }
 
        return PokerAction.Call;
    }
 


    // Hand-strength score (0 = weakest, 1 = strongest) used by DecideAction.
    private float EvaluateHandStrength(List<PlayingCard> communityCards, List<PlayingCard> hand)
    {
        if (hand == null || hand.Count == 0) return 0f;
 
        int communityCount = communityCards != null ? communityCards.Count : 0;
        int totalCards = hand.Count + communityCount;
 
        if (totalCards < 5)
        {
            return EvaluatePreflopStrength(hand);
        }
 
        PokerScore score = PokerManager.Instance.EvaluateScore(communityCards, hand);
        return NormalizeScore(score);
    }
 
    // Converts a real PokerScore (hand type + tiebreaker cards) into the
    // 0..1 range DecideAction expects.
    private float NormalizeScore(PokerScore score)
    {
        int[] handTypeValues = (int[])Enum.GetValues(typeof(PokerHand));
        int minHandValue = handTypeValues.Min();
        int maxHandValue = handTypeValues.Max();
        int handValue = (int)score.pokerHand;
 
        float handTypeScore = maxHandValue > minHandValue
            ? (float)(handValue - minHandValue) / (maxHandValue - minHandValue)
            : 0f;
 
        // normalize tiebreaker card
        float kickerScore = 0f;
        if (score.pokerCards != null && score.pokerCards.Count > 0)
        {
            kickerScore = score.pokerCards[0] / 14f;
        }
 
        // give the handtype the majority of the score and the kicker a small percentage to break ties
        float strength = (handTypeScore * 0.9f) + (kickerScore * 0.1f);
 
        return Mathf.Clamp01(strength);
    }
 
    // Preflop-only fallback: with just 2 hole cards there's no real poker
    private float EvaluatePreflopStrength(List<PlayingCard> hand)
    {
        int highCard = 0;
        bool isPocketPair = hand.Count >= 2 && hand[0].cardValue == hand[1].cardValue;
 
        foreach (PlayingCard card in hand)
        {
            if (card.cardValue > highCard)
            {
                highCard = card.cardValue;
            }
        }
 
        // Normalize high card the same way as postflop 
        float highCardScore = highCard / 14f;
 
        // With only 2 hole cards, it's either a pocket pair or it isn't
        float pairScore = isPocketPair ? 1f : 0f;
 
        float strength = (highCardScore * 0.4f) + (pairScore * 0.6f);
 
        return Mathf.Clamp01(strength);
    }
}