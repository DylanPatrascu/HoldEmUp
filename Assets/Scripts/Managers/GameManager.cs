using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [DoNotSerialize]
    private static StateMachine _stateMachine = new();

    public State CurrentState { get { return _stateMachine.CurrentState; } }

    private static GameManager instance;

    private static Dictionary<State, string> stateScenes = new()
    {
        [State.Menu] = "Assets/Scenes/MainMenu.unity",
        [State.InClub] = "Assets/Scenes/TestClubScene.unity",
        [State.FirstPerson] = "Assets/Scenes/TestFirstPersonScene.unity"
    };

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.LoadScene(stateScenes[State.Menu]);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _stateMachine.Enter += handleSceneChange;
        _stateMachine.Exit  += handleSceneUnload;
    }

    public static void handleSceneChange(object sender, StateEventArgs e)
    {
        // Handle Cursor Locks
        if (e.target == State.FirstPerson)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        } else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // If it's a Pause screen then we don't need to load a new scene
        if (e.target == State.Paused) return;

        string sceneToLoad = stateScenes[e.target];
        SceneManager.LoadSceneAsync(sceneToLoad);
    }

    public static void handleSceneUnload(object sender, StateEventArgs e)
    {
        if (e.target == State.Paused) return;

        string sceneToUnload = stateScenes[e.target];
        SceneManager.UnloadSceneAsync(sceneToUnload);
        Debug.Log($"Unloaded scene {e.target}");
    }

    public void Fire(Trigger trigger)
    {
        try
        {
            _stateMachine.Fire(trigger);
        } catch (System.InvalidOperationException exception)
        {
            Debug.LogWarning(exception.Message);
        }
    }
}
