using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static StateMachine _stateMachine = new();

    public State CurrentState { get { return _stateMachine.CurrentState; } }

    public int PlayerBalance = BettingManager.STARTING_BALANCE;
    public int PokerRound = 0;

    public static GameManager Instance;

    [SerializeField]
    private GameObject pauseGameUI;
    public PauseMenu PauseGameUI => pauseGameUI.GetComponent<PauseMenu>();

    public bool IsGamePaused { get; private set; } = false;

    private static Dictionary<State, string> stateScenes = new()
    {
        [State.Menu] = "Assets/Scenes/MainMenu.unity",
        [State.InClub] = "Assets/Scenes/ClubScene.unity",
        [State.FirstPerson] = "Assets/Scenes/PokerScene.unity"
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

        pauseGameUI = Instantiate(pauseGameUI, GameObject.Find("Canvas").transform);
        pauseGameUI.SetActive(false);

        // SceneManager.LoadScene(stateScenes[CurrentState]);
    }

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
        pauseGameUI.SetActive(true);
        Time.timeScale = 0f;
        IsGamePaused = true;

        if (CurrentState == State.FirstPerson)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ResumeGame()
    {
        pauseGameUI.SetActive(false);
        Time.timeScale = 1f;
        IsGamePaused = false;

        if (CurrentState == State.FirstPerson)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
}
