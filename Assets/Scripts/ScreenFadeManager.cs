using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFadeManager : MonoBehaviour
{
    public static ScreenFadeManager Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultFadeDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Setup fade image
        if (fadeImage == null)
        {
            // Create canvas and image if not assigned
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("FadeCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;
                
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvas.transform, false);
            fadeImage = imageObj.AddComponent<Image>();
            
            RectTransform rect = fadeImage.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
    }

    public void StartFadeToBlack(float duration = -1)
    {
        float fadeDuration = duration > 0 ? duration : defaultFadeDuration;
        StartCoroutine(FadeToBlack(fadeDuration));
    }

    public void StartFadeFromBlack(float duration = -1)
    {
        float fadeDuration = duration > 0 ? duration : defaultFadeDuration;
        StartCoroutine(FadeFromBlack(fadeDuration));
    }

    private IEnumerator FadeToBlack(float duration)
    {
        float timer = 0f;
        Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        Color endColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        float timer = 0f;
        Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        Color endColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            fadeImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        fadeImage.color = endColor;
    }

    // Quick fade for instant effects
    public void FadeToBlackInstant()
    {
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
    }

    public void FadeFromBlackInstant()
    {
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
    }
}
