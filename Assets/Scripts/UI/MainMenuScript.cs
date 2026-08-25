using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuScript : MonoBehaviour
{
    [SerializeField]
    private GameManager gameManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // ClickSFX = GetComponent<AudioSource>();
        if (!gameManager) gameManager = FindFirstObjectByType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void _load_game()
    {
        gameManager.BeginGame();
    }
    public void _quit_game()
    {
        Application.Quit();
    }
}
