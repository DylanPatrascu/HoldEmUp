using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuScript : MonoBehaviour
{
    private AudioSource ClickSFX;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ClickSFX = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public async void _load_game()
    {
        ClickSFX.Play();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 3);
    }
    public void _quit_game()
    {
        Application.Quit();
    }
}
