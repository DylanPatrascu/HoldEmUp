using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static StateMachine _stateMachine = new();

    public State CurrentState { get { return _stateMachine.CurrentState; } }

    public int PlayerBalance = BettingManager.STARTING_BALANCE;
    public int GameRound = 0;
    public PlayerInput PlayerInputSystem;
    public static GameManager Instance;

    [SerializeField] private GameObject pauseGameUI;
    [SerializeField] private PauseSystem pauseSystem;
    public PauseMenu PauseGameUI => pauseGameUI != null ? pauseGameUI.GetComponent<PauseMenu>() : null;
    public bool IsGamePaused => pauseSystem != null ? pauseSystem.IsPaused : false;

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

    [Space]
    [SerializeField] private GameObject FaderObject;
    [SerializeField] private SceneTransitionController sceneTransitionController;
    private Image FaderImage => FaderObject != null ? FaderObject.GetComponent<Image>() : null;

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

        if (pauseSystem == null)
        {
            pauseSystem = GetComponent<PauseSystem>();
        }

        if (pauseSystem == null)
        {
            pauseSystem = gameObject.AddComponent<PauseSystem>();
        }

        pauseSystem.Initialize(this, PlayerInputSystem, pauseGameUI, state => stateActionMaps[state]);

        if (sceneTransitionController == null)
        {
            sceneTransitionController = GetComponent<SceneTransitionController>();
        }

        if (sceneTransitionController == null)
        {
            sceneTransitionController = gameObject.AddComponent<SceneTransitionController>();
        }

        if (FaderObject != null)
        {
            sceneTransitionController.Initialize(FaderObject);
        }

        if (pauseGameUI != null) pauseGameUI.SetActive(false);

        StartCoroutine(LoadNextScene(CurrentState));
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        if (pauseSystem != null) pauseSystem.Shutdown();
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

        string targetMap = stateActionMaps[e.target];
        if (PlayerInputSystem.currentActionMap == null || PlayerInputSystem.currentActionMap.name != targetMap)
        {
            PlayerInputSystem.SwitchCurrentActionMap(targetMap);
        }

        if (e.target == State.Menu)
        {
            ResumeGame();
            PlayerBalance = 100;
            GameRound = 0;
        }

        StartCoroutine(LoadNextScene(e.target));
    }

    public IEnumerator LoadNextScene(State target)
    {
        if (sceneTransitionController != null)
        {
            if (CurrentState != State.Menu)
            {
                sceneTransitionController.SetFaderActive(true);
                yield return StartCoroutine(sceneTransitionController.SceneTransition("fadeIn", 1f));
            }
            else
            {
                sceneTransitionController.SetFaderActive(true, true);
                sceneTransitionController.SetChipActive(true);
            }
        }

        if (target == State.InClub)
        {
            StartNewClubRound(PlayerBalance);
            if (TwoDSceneUI != null) TwoDSceneUI.SetActive(true);
        }
        else
        {
            if (TwoDSceneUI != null) TwoDSceneUI.SetActive(false);
        }

        string sceneToLoad = stateScenes[target];
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneToLoad);
        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f) yield return null;

        operation.allowSceneActivation = true;

        while (!operation.isDone) yield return null;

        if (sceneTransitionController != null)
        {
            yield return StartCoroutine(sceneTransitionController.SceneTransition("fadeOut", 1f));
            sceneTransitionController.SetFaderActive(false);
        }
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

    public void PauseGame()
    {
        if (pauseSystem != null)
        {
            pauseSystem.PauseGame();
            return;
        }

        if (pauseGameUI != null) pauseGameUI.SetActive(true);
        GeneralPause();

        if (CurrentState == State.FirstPerson)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ResumeGame()
    {
        if (pauseSystem != null)
        {
            pauseSystem.ResumeGame();
            return;
        }

        if (pauseGameUI != null) pauseGameUI.SetActive(false);
        GeneralResume();

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
        GameRound++;
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
        return CurrentQuestionsAskedThisRound < maxQuestionsPerRound[GameRound];
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

    public void UpdateClubRoundText()
    {
        questionText.text = (maxQuestionsPerRound[GameRound] - CurrentQuestionsAskedThisRound).ToString();
        Debug.Log($"{maxQuestionsPerRound[GameRound]} / {CurrentQuestionsAskedThisRound}");
        chipsText.text = PokerChipsAvailable.ToString();
    }

    public void DisplayConfirmationMenu(string textToShow = "Ready to keep playing?")
    {
        confirmationMenu.transform.Find("ConfirmationText").GetComponent<TextMeshProUGUI>().text = textToShow;
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
