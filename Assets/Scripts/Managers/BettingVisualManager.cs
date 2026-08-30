using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class BettingVisualManager : MonoBehaviour
{
    public static BettingVisualManager Instance { get; private set; }

    [SerializeField] private TMP_Text currentBetText;


    [Header("Chips and spawning them")]
    [SerializeField]
    private GameObject oneChip;
    [SerializeField]
    private GameObject tenChip;

    [SerializeField]
    private ChipStacks playerStack;
    [SerializeField]
    private ChipStacks playerTable;

    [Header("Chip Movement")]
    [SerializeField] private float liftHeight = 0.3f;
    [SerializeField] private float liftDuration = 0.15f;


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
        PokerGameManager.Instance.PerformedPlayerAction += PlaySounds;
        PokerGameManager.Instance.PerformedPlayerAction += DisplaySentence;

    }

    private void DisplaySentence(object sender, PokerGameManager.PokerEvent e)
    {
        switch (e.Action)
        {
            case PokerAction.Fold:
                PokerVisualManager.Instance.DisplaySentence($"{e.Player} folded.");
                break;
            case PokerAction.Check:
                PokerVisualManager.Instance.DisplaySentence($"{e.Player} checked.");
                break;
            case PokerAction.Bet:
                PokerVisualManager.Instance.DisplaySentence($"{e.Player} bet {e.Amount} chips.");
                break;
            case PokerAction.Raise:
                PokerVisualManager.Instance.DisplaySentence($"{e.Player} raised {e.Amount} chips.");
                break;
            case PokerAction.Call:
                PokerVisualManager.Instance.DisplaySentence($"{e.Player} called.");
                break;
        }
    }

    private void PlaySounds(object sender, PokerGameManager.PokerEvent e)
    {
        if (e.Player == PokerPosition.Joker || e.Player == PokerPosition.Table) return;
        if (e.Action == PokerAction.Fold)
        {
            AudioManager.Instance.PlayAudioClip(AudioSnippet.PlayingCardFold);
        }
        else
        {
            AudioManager.Instance.PlayAudioClip(AudioSnippet.PokerChip);
        } 
    }
    
    public void UpdateBet(int bet)
    {
        currentBetText.text = bet.ToString();
    }

    public void SpawnChips(int amount, ChipLocation location, bool locked = false)
    {
        int onesPile = amount % 10;
        int tensPile = (amount - onesPile) / 10;
        ChipStacks chipStacks = location == ChipLocation.Stack ? playerStack : playerTable;
 
        if (onesPile == 0)
        {
            tensPile--;
            onesPile = 10;
        }
 
        chipStacks.ClearStacks();
 
        for (int i = 0; i < onesPile; i++)
        {
            SpawnChipAt(oneChip, 1, chipStacks.Ones, i, location, locked);
        }
        for (int i = 0; i < tensPile; i++)
        {
            SpawnChipAt(tenChip, 10, chipStacks.Tens, i, location, locked);
        }
    }

    private GameObject SpawnChipAt(GameObject prefab, int value, Transform pile, int stackIndex, ChipLocation location, bool locked = false)
    {
        Vector3 spawnPosition = pile.position + Vector3.up * (stackIndex * 0.05f);
 
        GameObject spawned = Instantiate(prefab, spawnPosition, Quaternion.identity, pile);
 
        Chip chip = spawned.GetComponent<Chip>();
        if (chip != null)
        {
            chip.chipValue = value;
            chip.location = location;
            chip.SetLocked(locked);
        }
 
        return spawned;
    }

    public void MoveChip(Chip chip, ChipLocation destination)
    {
        StartCoroutine(AnimateChipMove(chip, destination));
    }
 
    private IEnumerator AnimateChipMove(Chip chip, ChipLocation destination)
    {
        chip.SetLocked(true);
 
        Vector3 startPosition = chip.transform.position;
        Vector3 liftedPosition = startPosition + Vector3.up * liftHeight;
        float elapsed = 0f;
 
        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            chip.transform.position = Vector3.Lerp(startPosition, liftedPosition, elapsed / liftDuration);
            yield return null;
        }
 
        int value = chip.chipValue;
        GameObject prefab = value == 10 ? tenChip : oneChip;
        ChipStacks destinationStack = destination == ChipLocation.Stack ? playerStack : playerTable;
        Transform pile = value == 10 ? destinationStack.Tens : destinationStack.Ones;
 
        Destroy(chip.gameObject);
 
        SpawnChipAt(prefab, value, pile, pile.childCount, destination);
    }

    public void SetChipsLocked(ChipLocation location, bool locked)
    {
        ChipStacks chipStacks = location == ChipLocation.Stack ? playerStack : playerTable;
        foreach (Chip chip in chipStacks.GetComponentsInChildren<Chip>())
        {
            chip.SetLocked(locked);
        }
    }

    public void SetAllChipsLocked(bool locked)
    {
        SetChipsLocked(ChipLocation.Stack, locked);
        SetChipsLocked(ChipLocation.Table, locked);
    }

}
