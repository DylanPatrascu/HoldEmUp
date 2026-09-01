using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneTransitionController : MonoBehaviour
{
    [SerializeField] private GameObject faderObject;

    private Image faderImage;

    public GameObject FaderObject => faderObject;

    private void Awake()
    {
        CacheReferences();
    }

    public void Initialize(GameObject fader)
    {
        faderObject = fader;
        CacheReferences();
    }

    private void CacheReferences()
    {
        if (faderObject == null) return;

        faderImage = faderObject.GetComponent<Image>();
        SetChipActive(false);
    }

    public void SetFaderActive(bool active, bool overrideAlpha = false)
    {
        if (faderObject == null) return;

        if (faderImage != null)
        {
            Color color = faderImage.color;
            color.a = Mathf.Clamp01(active && !overrideAlpha ? 0f : 1f);
            faderImage.color = color;
        }

        faderObject.SetActive(active);
        SetChipActive(!active);
    }

    public void SetChipActive(bool active)
    {
        if (faderObject == null) return;

        Transform loadingChip = faderObject.transform.Find("LoadingChip");
        if (loadingChip != null) loadingChip.gameObject.SetActive(active);
    }

    public IEnumerator SceneTransition(string animationType, float duration)
    {
        bool isFadeIn = animationType == "fadeIn";
        float startAlpha = isFadeIn ? 0f : 1f;
        float endAlpha = isFadeIn ? 1f : 0f;

        yield return StartCoroutine(FadeUI(startAlpha, endAlpha, duration));

        SetChipActive(isFadeIn);
    }

    public IEnumerator FadeUI(float startAlpha, float endAlpha, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            if (faderImage == null) yield break;

            Color color = faderImage.color;
            color.a = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            faderImage.color = color;
            yield return null;
        }

        if (faderImage == null) yield break;

        Color finalColor = faderImage.color;
        finalColor.a = endAlpha;
        faderImage.color = finalColor;
    }
}
