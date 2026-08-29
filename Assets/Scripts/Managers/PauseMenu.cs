using System;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public enum MenuState
    {
        PauseMenu,
        Settings,
        Hands,
        CaseFile,
    };
    public Stack<MenuState> PrevStates;
    public MenuState CurrentMenuState = MenuState.PauseMenu;

    public EventHandler ResumeRequested;

    public void ResumeGame()
    { ResumeRequested?.Invoke(this, EventArgs.Empty); }

    public void OpenSettings()
    {
        PrevStates.Push(CurrentMenuState);
        CurrentMenuState = MenuState.Settings;
    }

    public void OpenHands()
    {
        PrevStates.Push(CurrentMenuState);
        CurrentMenuState = MenuState.Hands;
    }

    public void OpenCaseFile()
    {
        PrevStates.Push(CurrentMenuState);
        CurrentMenuState = MenuState.CaseFile;
    }

    public void GoToMainMenu()
    {
        GameManager.Instance.Fire(Trigger.ToMainMenu);
    }

    public void GoBack()
    {
        CurrentMenuState = PrevStates.Pop();
    }
}
