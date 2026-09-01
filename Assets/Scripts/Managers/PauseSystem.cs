using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseSystem : MonoBehaviour
{
    private GameManager gameManager;
    private PlayerInput playerInput;
    private GameObject pauseGameUI;

    private InputAction pauseAction;
    private InputAction cancelAction;

    private float lastPauseToggleTime = -1f;
    private const float PAUSE_TOGGLE_COOLDOWN = 0.2f;

    public bool IsPaused { get; private set; }

    public void Initialize(GameManager manager, PlayerInput inputSystem, GameObject pauseUi, Func<State, string> getActionMapForState)
    {
        gameManager = manager;
        playerInput = inputSystem;
        pauseGameUI = pauseUi;

        if (playerInput == null) return;

        var playerMap = playerInput.actions.FindActionMap("Player");
        var uiMap = playerInput.actions.FindActionMap("UI");

        pauseAction = playerMap != null ? playerMap.FindAction("Pause") : null;
        cancelAction = uiMap != null ? uiMap.FindAction("Cancel") : null;

        if (pauseAction != null) pauseAction.performed += OnPausePerformed;
        if (cancelAction != null) cancelAction.performed += OnCancelPerformed;

        this.getActionMapForState = getActionMapForState;
    }

    private Func<State, string> getActionMapForState;

    public void Shutdown()
    {
        if (pauseAction != null) pauseAction.performed -= OnPausePerformed;
        if (cancelAction != null) cancelAction.performed -= OnCancelPerformed;

        pauseAction = null;
        cancelAction = null;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        if (IsPaused || gameManager.CurrentState == State.Menu) return;
        if (Time.unscaledTime - lastPauseToggleTime < PAUSE_TOGGLE_COOLDOWN) return;
        lastPauseToggleTime = Time.unscaledTime;

        PauseGame();

        if (playerInput != null) playerInput.SwitchCurrentActionMap("UI");
    }

    private void OnCancelPerformed(InputAction.CallbackContext ctx)
    {
        if (!IsPaused) return;
        if (Time.unscaledTime - lastPauseToggleTime < PAUSE_TOGGLE_COOLDOWN) return;
        lastPauseToggleTime = Time.unscaledTime;

        ResumeGame();

        if (playerInput != null && gameManager != null && getActionMapForState != null)
        {
            playerInput.SwitchCurrentActionMap(getActionMapForState(gameManager.CurrentState));
        }
    }

    public void PauseGame()
    {
        if (pauseGameUI != null) pauseGameUI.SetActive(true);
        Time.timeScale = 0f;
        IsPaused = true;

        if (gameManager != null && gameManager.CurrentState == State.FirstPerson)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ResumeGame()
    {
        if (pauseGameUI != null) pauseGameUI.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;

        if (gameManager != null && gameManager.CurrentState == State.FirstPerson)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
