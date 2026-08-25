using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [DoNotSerialize]
    public StateMachine _stateMachine = new();

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
        SceneManager.LoadScene(stateScenes[State.FirstPerson]);
    }

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

    public void BeginGame()
    {
        try
        {
            _stateMachine.Fire(Trigger.ToClub);
        } catch (System.InvalidOperationException exception)
        {
            Debug.LogWarning(exception.Message);
        }
    }
}
