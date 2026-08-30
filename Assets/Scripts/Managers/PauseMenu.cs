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
    public List<MenuState> PrevStates;
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

    public void ResumeGame()
    { ResumeRequested?.Invoke(this, EventArgs.Empty); }

    private void ActivateCurrentObject()
    {
        foreach (MenuObject m in options)
        {
            if (m.Menu == CurrentMenuState && !m.UIComponent.activeSelf)
                m.UIComponent.SetActive(true);
            else if (m.Menu != CurrentMenuState && m.UIComponent.activeSelf)
                m.UIComponent.SetActive(false);
        }
    }

    public void OpenSettings()
    {
        PrevStates.Add(CurrentMenuState);
        CurrentMenuState = MenuState.Settings;

        ActivateCurrentObject();
    }

    public void OpenHands()
    {
        PrevStates.Add(CurrentMenuState);
        CurrentMenuState = MenuState.Hands;

        ActivateCurrentObject();
    }

    public void OpenCaseFile()
    {
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
        CurrentMenuState = PrevStates[0];
        PrevStates.RemoveAt(0);

        ActivateCurrentObject();
    }
}
