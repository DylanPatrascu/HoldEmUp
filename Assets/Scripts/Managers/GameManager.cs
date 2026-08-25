using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [DoNotSerialize]
    public StateMachine _stateMachine = new();

    private static Dictionary<State, string> stateScenes = new()
    {
        [State.Menu] = "Assets/Scenes/SampleScene",
        [State.InClub] = "Assets/Scenes/SampleScene",
        [State.FirstPerson] = "Assets/Scenes/SampleScene"
    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _stateMachine.Enter += unlockCursor;
        _stateMachine.Enter += lockCursor;
        _stateMachine.Enter += handleSceneChange;
        _stateMachine.Exit  += handleSceneUnload;
    }

    public static void handleSceneChange(object sender, StateEventArgs e)
    {
        if (e.target == State.Paused) return;

        string sceneToLoad = stateScenes[e.target];
        var op = SceneManager.LoadSceneAsync(sceneToLoad);
        op.completed += (AsyncOperation obj) =>
        {
            Scene loadedScene = SceneManager.GetSceneByPath(sceneToLoad);
            Debug.Log($"{stateScenes} {e.target} finished loading (build index: {loadedScene.buildIndex}).");
            Debug.Log($"It has {loadedScene.rootCount} root(s).");
            Debug.Log($"There are now {SceneManager.loadedSceneCount} Scenes open.");
        };
    }

    public static void handleSceneUnload(object sender, StateEventArgs e)
    {
        if (e.target == State.Paused) return;

        string sceneToUnload = stateScenes[e.target];
        SceneManager.UnloadSceneAsync(sceneToUnload);
        Debug.Log($"Unloaded scene {e.target}");
    }

    public static void unlockCursor(object sender, StateEventArgs e)
    {
       if (e.target == State.FirstPerson) return;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void lockCursor(object sender, StateEventArgs e)
    {
       if (e.target != State.FirstPerson) return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        GUILayout.BeginArea(new Rect(12f, 12f, 220f, 300f), GUI.skin.window);
        GUILayout.Label($"State: {_stateMachine.CurrentState}");
        DrawTriggerButton("To Club", Trigger.ToClub);
        DrawTriggerButton("To First Person", Trigger.ToFirstPerson);
        DrawTriggerButton("To Main Menu", Trigger.ToMainMenu);
        DrawTriggerButton("Pause", Trigger.Pause);
        DrawTriggerButton("Resume", Trigger.Resume);
        GUILayout.EndArea();
    }

    private void DrawTriggerButton(string label, Trigger trigger)
    {
        if (!GUILayout.Button(label)) return;

        try
        {
            _stateMachine.Fire(trigger);
        }
        catch (System.InvalidOperationException exception)
        {
            Debug.LogWarning(exception.Message);
        }
    }
}
