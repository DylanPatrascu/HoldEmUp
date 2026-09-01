using UnityEngine;
using UnityEngine.UI;

public class GameOverScript : MonoBehaviour
{
    [SerializeField] private bool playerWon = false;
    [SerializeField] private Button returnButton;

    private void Awake()
    {
        if (returnButton != null)
        {
            returnButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    public void SetOutcome(bool won)
    {
        playerWon = won;
    }

    public void ReturnToMainMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Fire(Trigger.ToMainMenu);
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // Legacy Unity button callback support.
    public void _return_to_menu()
    {
        ReturnToMainMenu();
    }
}
