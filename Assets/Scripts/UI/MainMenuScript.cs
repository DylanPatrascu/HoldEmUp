using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuScript : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // ClickSFX = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void _load_game()
    {
        GameManager._stateMachine.Fire(Trigger.ToClub);
    }
    public void _quit_game()
    {
        Application.Quit();
    }
}
