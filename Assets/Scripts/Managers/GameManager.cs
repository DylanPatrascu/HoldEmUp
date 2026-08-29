using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static StateMachine _stateMachine = new();

    public State CurrentState { get { return _stateMachine.CurrentState; } }

    // Input System Actions
    public InputActions Controls;

    public int PlayerBalance = BettingManager.STARTING_BALANCE;
    public int PokerRound = 0;

    public static GameManager Instance;

    private static Dictionary<State, string> stateScenes = new()
    {
        [State.Menu] = "Assets/Scenes/MainMenu.unity",
        [State.InClub] = "Assets/Scenes/TestClubScene.unity",
        [State.FirstPerson] = "Assets/Scenes/TestFirstPersonScene.unity"
    };

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Controls = new InputActions();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // SceneManager.LoadScene(stateScenes[CurrentState]);
    }

    void OnEnable()
    { Controls.Enable(); }

    void OnDisable()
    { Controls.Disable(); }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _stateMachine.Enter += handleSceneChange;
        _stateMachine.Exit  += handleSceneChange;
        // _stateMachine.Fire(Trigger.ToFirstPerson);
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

        string sceneToLoad = stateScenes[e.target];
        SceneManager.LoadSceneAsync(sceneToLoad);
    }

    public static void handleSceneUnload(object sender, StateEventArgs e)
    {
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

    public void PauseGame()
    {
        Time.timeScale = 0f;
    }
    public void UnpauseGame()
    {
        Time.timeScale = 1f;
    }
    
}
