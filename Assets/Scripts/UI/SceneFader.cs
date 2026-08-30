using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 1f;

    private void Start()
    {
        fadeImage.enabled = true;
        StartCoroutine(FadeIn());
    }

    public void FadeToScene(string sceneName)
    {
        if (Time.timeScale == 0)
        {
            return;
        }
        //StopAllCoroutines();
        StartCoroutine(FadeOut(sceneName));
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        StartCoroutine(FadeOut("MainMenu"));

    }

    public void FadeToQuit()
    {
        if (Time.timeScale == 0)
        {
            return;
        }
        //StopAllCoroutines();
        StartCoroutine(FadeOutAndQuit());
    }

    IEnumerator FadeIn()
    {
        float t = fadeDuration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            float alpha = t / fadeDuration;
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(0f);
        fadeImage.enabled = false;
    }

    IEnumerator FadeOut(string sceneName)
    {
        fadeImage.enabled = true;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = t / fadeDuration;
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(1f);
        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeOutAndQuit()
    {
        fadeImage.enabled = true;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = t / fadeDuration;
            SetAlpha(alpha);
            yield return null;
        }
        SetAlpha(1f);
        Application.Quit();
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = Mathf.Clamp01(alpha);
            fadeImage.color = c;
        }
    }
}