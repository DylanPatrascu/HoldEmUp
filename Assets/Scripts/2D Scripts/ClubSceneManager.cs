using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ClubSceneManager : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] private int maxQuestionsPerRound = 3;
    
    [Space]
    [Header("UI Elements")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text chipsText;
    [SerializeField] private GameObject confirmationMenu;
    
    public int CurrentQuestionsAskedThisRound { get; private set; }
    public int PokerChipsAvailable { get; private set; }

    [SerializeField] public int CurrentClubRound = 0;
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public void Start()
    {
        // temp until game manager handles this
        StartNewClubRound(300);
    }

    public void StartNewClubRound(int pokerChips)
    {
        PokerChipsAvailable = pokerChips;
        CurrentQuestionsAskedThisRound = 0;
        HideConfirmationMenu();
        UpdateText();
        CurrentClubRound++;
    }

    public void EndCurrentClubRound()
    {
        print("End Current Club Round");
        GameManager.Instance.Fire(Trigger.ToFirstPerson);
    }

    public bool CanAskQuestion()
    {
        return CurrentQuestionsAskedThisRound < maxQuestionsPerRound;
    }

    public bool CanAffordBribe(int bribeAmount)
    {
        return PokerChipsAvailable >= bribeAmount;
    }

    public void Bribed(int bribeAmount)
    {
        if (!CanAffordBribe(bribeAmount)) Debug.LogError("Can't afford Bribe");
        PokerChipsAvailable -= bribeAmount;
        UpdateText();
    }

    public void AskedAQuestion()
    {
        CurrentQuestionsAskedThisRound++;
        UpdateText();
    }

    private void UpdateText()
    {
        questionText.text = (maxQuestionsPerRound - CurrentQuestionsAskedThisRound).ToString();
        chipsText.text = PokerChipsAvailable.ToString();
    }

    // TODO: Figure out game manager singleton thats not in current scene (persist?)
    public void DisplayConfirmationMenu()
    {
        confirmationMenu.SetActive(true);
        Pause();
    }

    public void HideConfirmationMenu()
    {
        Resume();
        confirmationMenu.SetActive(false);
    }

    public void Pause()
    {
        Time.timeScale = 0;
    }

    public void Resume()
    {
        Time.timeScale = 1;
    }
}
