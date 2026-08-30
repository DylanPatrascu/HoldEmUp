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
    public PlayerInput PlayerInputSystem;
    public static GameManager Instance;

    [SerializeField]
    private GameObject pauseGameUI;
    public PauseMenu PauseGameUI => pauseGameUI.GetComponent<PauseMenu>();
    public bool IsGamePaused { get; private set; } = false;

    private static readonly Dictionary<State, string> stateActionMaps = new()
    {
        [State.Menu] = "UI",
        [State.InClub] = "Player",
        [State.FirstPerson] = "Player"
    };

    private static Dictionary<State, string> stateScenes = new()
    {
        [State.Menu] = "Assets/Scenes/MainMenu.unity",
        [State.InClub] = "Assets/Scenes/ClubScene.unity",
        [State.FirstPerson] = "Assets/Scenes/PokerScene.unity"
    };

    // Cached so we can unsubscribe cleanly; these live for the app's lifetime
    // since GameManager is never destroyed once Instance is set.
    private InputAction pauseAction;
    private InputAction cancelAction;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (PlayerInputSystem == null) PlayerInputSystem = GetComponent<PlayerInput>();

        pauseAction = PlayerInputSystem.actions.FindAction("Pause");
        cancelAction = PlayerInputSystem.actions.FindAction("Cancel");

        if (pauseAction != null) pauseAction.performed += OnPausePerformed;
        if (cancelAction != null) cancelAction.performed += OnCancelPerformed;

        SceneManager.LoadScene(stateScenes[CurrentState]);
    }

    void OnDestroy()
    {
        if (Instance != this) return;

        if (pauseAction != null) pauseAction.performed -= OnPausePerformed;
        if (cancelAction != null) cancelAction.performed -= OnCancelPerformed;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _stateMachine.Enter += handleSceneChange;

        _stateMachine.Fire(Trigger.ToClub);
    }

    public void handleSceneChange(object sender, StateEventArgs e)
    {
        // Handle Cursor Locks
        if (e.target == State.FirstPerson)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        string sceneToLoad = stateScenes[e.target];
        SceneManager.LoadScene(sceneToLoad);

        PlayerInputSystem.SwitchCurrentActionMap(stateActionMaps[e.target]);

        pauseGameUI = Instantiate(pauseGameUI, GameObject.Find("Canvas").transform);
        pauseGameUI.SetActive(false);
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

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (IsGamePaused) return;

        PauseGame();
        PlayerInputSystem.SwitchCurrentActionMap("UI");
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsGamePaused) return;

        ResumeGame();
        PlayerInputSystem.SwitchCurrentActionMap(stateActionMaps[CurrentState]);
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
