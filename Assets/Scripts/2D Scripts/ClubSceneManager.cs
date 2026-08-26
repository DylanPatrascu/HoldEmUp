using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class ClubSceneManager : MonoBehaviour
{
    [SerializeField] private int maxQuestionsPerRound = 3;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text chipsText;
    
    public int CurrentQuestionsAskedThisRound { get; private set; }
    public int PokerChipsAvailable { get; private set; }

    public void Start()
    {
        // temp until game manager handles this
        StartNewClubRound(300);
    }

    public void StartNewClubRound(int pokerChips)
    {
        PokerChipsAvailable = pokerChips;
        CurrentQuestionsAskedThisRound = 0;
        UpdateText();
    }

    private void EndCurrentClubRound()
    {
        // pass off remaining chips to game manager and switch scenes
    }

    public bool CanAskQuestion()
    {
        return CurrentQuestionsAskedThisRound < maxQuestionsPerRound;
    }

    public void AskedAQuestion()
    {
        CurrentQuestionsAskedThisRound++;
        UpdateText();
    }

    private void UpdateText()
    {
        questionText.text = "Q's Remaining: " + (maxQuestionsPerRound - CurrentQuestionsAskedThisRound);
        chipsText.text = "Chips: " + PokerChipsAvailable;
    }
}
