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
    public List<MenuState> PrevStates = new List<MenuState>();
    public MenuState CurrentMenuState = MenuState.PauseMenu;

    [Serializable]
    public struct MenuObject
    {
        public MenuState Menu;
        public GameObject UIComponent;
    }

    [SerializeField]
    private List<MenuObject> options;

    public EventHandler ResumeRequested;

    void OnEnable()
    {
        if (options == null) return;

        foreach (MenuObject m in options)
        {
            if (m.UIComponent == null) continue;
            if (m.Menu == MenuState.Hands && GameManager.Instance != null)
                m.UIComponent.SetActive(GameManager.Instance.CurrentState == State.FirstPerson);
        }
    }

    public void ResumeGame()
    { ResumeRequested?.Invoke(this, EventArgs.Empty); }

    private void ActivateCurrentObject()
    {
        if (options == null) return;

        foreach (MenuObject m in options)
        {
            if (m.UIComponent == null) continue;

            if (m.Menu == CurrentMenuState && !m.UIComponent.activeSelf)
                m.UIComponent.SetActive(true);
            else if (m.Menu != CurrentMenuState && m.UIComponent.activeSelf)
                m.UIComponent.SetActive(false);
        }
    }

    public void OpenSettings()
    {
        if (PrevStates == null) PrevStates = new List<MenuState>();
        PrevStates.Add(CurrentMenuState);
        CurrentMenuState = MenuState.Settings;

        ActivateCurrentObject();
    }

    public void OpenHands()
    {
        if (PrevStates == null) PrevStates = new List<MenuState>();
        PrevStates.Add(CurrentMenuState);
        CurrentMenuState = MenuState.Hands;

        ActivateCurrentObject();
    }

    public void OpenCaseFile()
    {
        if (PrevStates == null) PrevStates = new List<MenuState>();
        PrevStates.Add(CurrentMenuState);
        CurrentMenuState = MenuState.CaseFile;

        ActivateCurrentObject();
    }

    public void GoToMainMenu()
    {
        GameManager.Instance.Fire(Trigger.ToMainMenu);
    }

    public void GoBack()
    {
        if (PrevStates == null || PrevStates.Count == 0)
        {
            CurrentMenuState = MenuState.PauseMenu;
            ActivateCurrentObject();
            return;
        }

        CurrentMenuState = PrevStates[0];
        PrevStates.RemoveAt(0);

        ActivateCurrentObject();
    }
}
