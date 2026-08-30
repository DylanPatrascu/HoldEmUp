using System.Collections.Generic;
using System.Data;
using TMPro;
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

    // ---- Club round (formerly ClubSceneManager) ----
    [Space]
    [Header("Club Round Variables")]
    [SerializeField] private List<int> maxQuestionsPerRound = new List<int>();

    [Space]
    [Header("Club Round UI Elements")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text chipsText;
    [SerializeField] private GameObject confirmationMenu;
    [SerializeField] private GameObject TwoDSceneUI;

    public int CurrentQuestionsAskedThisRound { get; private set; }
    public int PokerChipsAvailable { get; private set; }

    // Action map to use for each game state. Centralized here so scene-specific
    // scripts never need to guess which map should be active.
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

    private float lastPauseToggleTime = -1f;
    private const float PAUSE_TOGGLE_COOLDOWN = 0.2f;

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

        pauseGameUI = Instantiate(pauseGameUI, transform);
        pauseGameUI.SetActive(false);

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

        string targetMap = stateActionMaps[e.target];
        if (PlayerInputSystem.currentActionMap == null || PlayerInputSystem.currentActionMap.name != targetMap)
        {
            PlayerInputSystem.SwitchCurrentActionMap(targetMap);
        }

        if (e.target == State.InClub)
        {
            StartNewClubRound(300);
            TwoDSceneUI.SetActive(true);
        } else
        {
            TwoDSceneUI.SetActive(false);
        }

        pauseGameUI.SetActive(false);
    }

    public void Fire(Trigger trigger)
    {
        try
        {
            _stateMachine.Fire(trigger);
        }
        catch (System.InvalidOperationException exception)
        {
            Debug.LogWarning(exception.Message);
        }
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (IsGamePaused) return;
        if (Time.unscaledTime - lastPauseToggleTime < PAUSE_TOGGLE_COOLDOWN) return;
        lastPauseToggleTime = Time.unscaledTime;

        PauseGame();
        PlayerInputSystem.SwitchCurrentActionMap("UI");
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsGamePaused) return;
        if (Time.unscaledTime - lastPauseToggleTime < PAUSE_TOGGLE_COOLDOWN) return;
        lastPauseToggleTime = Time.unscaledTime;

        ResumeGame();
        PlayerInputSystem.SwitchCurrentActionMap(stateActionMaps[CurrentState]);
    }

    public void PauseGame()
    {
        pauseGameUI.SetActive(true);
        GeneralPause();
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
        GeneralResume();
        IsGamePaused = false;

        if (CurrentState == State.FirstPerson)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ---- Club round (formerly ClubSceneManager) ----

    public void StartNewClubRound(int pokerChips)
    {
        PokerChipsAvailable = pokerChips;
        CurrentQuestionsAskedThisRound = 0;
        HideConfirmationMenu();
        UpdateClubRoundText();
        PokerRound++;
    }

    public void EndCurrentClubRound()
    {
        print("End Current Club Round");
        GeneralResume();
        HideConfirmationMenu();
        Fire(Trigger.ToFirstPerson);
    }

    public bool CanAskQuestion()
    {
        return CurrentQuestionsAskedThisRound < maxQuestionsPerRound[GameManager.Instance.PokerRound];
    }

    public bool CanAffordBribe(int bribeAmount)
    {
        return PokerChipsAvailable >= bribeAmount;
    }

    public void Bribed(int bribeAmount)
    {
        if (!CanAffordBribe(bribeAmount)) Debug.LogError("Can't afford Bribe");
        PokerChipsAvailable -= bribeAmount;
        UpdateClubRoundText();
    }

    public void AskedAQuestion()
    {
        CurrentQuestionsAskedThisRound++;
        UpdateClubRoundText();
    }

    private void UpdateClubRoundText()
    {
        questionText.text = (maxQuestionsPerRound[GameManager.Instance.PokerRound] - CurrentQuestionsAskedThisRound).ToString();
        Debug.Log($"{maxQuestionsPerRound[GameManager.Instance.PokerRound]} / {CurrentQuestionsAskedThisRound}");
        chipsText.text = PokerChipsAvailable.ToString();
    }

    public void DisplayConfirmationMenu()
    {
        confirmationMenu.SetActive(true);
        GeneralPause();
    }

    public void HideConfirmationMenu()
    {
        GeneralResume();
        confirmationMenu.SetActive(false);
    }

    public void GeneralPause()
    {
        Time.timeScale = 0f;
    }

    public void GeneralResume()
    {
        Time.timeScale = 1f;
    }
}
