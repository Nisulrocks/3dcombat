using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComboUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI comboText;
    [SerializeField] TextMeshProUGUI multiplierText;
    [SerializeField] Slider comboSlider;
    
    [Header("Visual Effects")]
    [SerializeField] Color[] comboColors = {Color.white, Color.yellow, Color.orange, Color.red, Color.purple, Color.cyan};
    [SerializeField] float popupDuration = 0.5f;
    [SerializeField] float fadeSpeed = 2f;
    [SerializeField] float popupScaleMultiplier = 1.5f;
    
    [Header("Random Positioning")]
    [SerializeField] Vector2 rightSidePositionRange = new Vector2(100, 300);
    [SerializeField] Vector2 verticalPositionRange = new Vector2(-200, 200);
    [SerializeField] Vector2 rotationRange = new Vector2(-15, 15);
    [SerializeField] float positionTransitionSpeed = 0.3f;
    [SerializeField] float autoHideDelay = 0.3f;
    
    [Header("Animation")]
    [SerializeField] AnimationCurve popupCurve;
    [SerializeField] AnimationCurve shakeCurve;

    [Header("Auto Hide")]
    [SerializeField] float maxVisibleDuration = 5f;
    
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Coroutine currentPopupCoroutine;
    private Coroutine currentSliderCoroutine;
    private Coroutine hideCoroutine;
    private Coroutine positionCoroutine;
    private Coroutine fadeOutCoroutine;
    private float visibleTimer = 0f;
    private bool isVisible = false;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        originalScale = transform.localScale;
        
        // Start visible but hidden (alpha = 0)
        canvasGroup.alpha = 0f;
        if (comboSlider != null)
            comboSlider.value = 0f;
    }
    
    private void Start()
    {
        // Subscribe to combo events if ComboManager exists
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.OnComboChanged += UpdateComboDisplay;
            ComboManager.Instance.OnComboWindowChanged += UpdateComboSlider;
        }

        // Subscribe to super events if SuperSystem exists
        if (SuperSystem.Instance != null)
        {
            SuperSystem.Instance.OnSuperActivated += ShowSuperActive;
            SuperSystem.Instance.OnSuperEnded += HideSuperActive;
        }
    }

    private void Update()
    {
        // Track visibility and auto-fade after max duration
        if (canvasGroup.alpha > 0.01f)
        {
            if (!isVisible)
            {
                isVisible = true;
                visibleTimer = 0f;
            }
            
            visibleTimer += Time.unscaledDeltaTime;
            
            // Auto-fade if visible for too long and not in active combo
            if (visibleTimer >= maxVisibleDuration)
            {
                bool inActiveCombo = ComboManager.Instance != null && 
                                     ComboManager.Instance.GetCurrentCombo() > 0 && 
                                     ComboManager.Instance.IsComboWindowActive();
                
                bool superActive = SuperSystem.Instance != null && SuperSystem.Instance.IsSuperActive;
                
                // Only auto-fade if not in active combo and super is not active
                if (!inActiveCombo && !superActive)
                {
                    if (fadeOutCoroutine != null)
                        StopCoroutine(fadeOutCoroutine);
                    fadeOutCoroutine = StartCoroutine(FadeOut());
                    visibleTimer = 0f;
                }
            }
        }
        else
        {
            isVisible = false;
            visibleTimer = 0f;
        }
    }
    
    private void OnDestroy()
    {
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.OnComboChanged -= UpdateComboDisplay;
            ComboManager.Instance.OnComboWindowChanged -= UpdateComboSlider;
        }

        if (SuperSystem.Instance != null)
        {
            SuperSystem.Instance.OnSuperActivated -= ShowSuperActive;
            SuperSystem.Instance.OnSuperEnded -= HideSuperActive;
        }
    }

    private void ShowSuperActive()
    {
        // Cancel any existing coroutines
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        if (positionCoroutine != null)
            StopCoroutine(positionCoroutine);
        if (currentPopupCoroutine != null)
            StopCoroutine(currentPopupCoroutine);

        // Show SUPER! text
        comboText.text = "SUPER!";
        multiplierText.text = "ACTIVE";
        
        // Set super color (orange/red)
        Color superColor = new Color(1f, 0.5f, 0f);
        comboText.color = superColor;
        multiplierText.color = superColor;

        // Generate random position
        GenerateRandomPositionAndRotation();
        positionCoroutine = StartCoroutine(MoveToPosition());
        currentPopupCoroutine = StartCoroutine(PopupAnimation());
    }

    private void HideSuperActive()
    {
        if (fadeOutCoroutine != null)
            StopCoroutine(fadeOutCoroutine);
        fadeOutCoroutine = StartCoroutine(FadeOut());
    }
    
    public void UpdateComboDisplay(int comboCount, float multiplier)
    {
        if (comboCount == 0)
        {
            // Combo reset - fade out immediately
            if (fadeOutCoroutine != null)
                StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = StartCoroutine(FadeOut());
            return;
        }
        
        // Cancel any existing coroutines (including fade out)
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        if (positionCoroutine != null)
            StopCoroutine(positionCoroutine);
        if (currentPopupCoroutine != null)
            StopCoroutine(currentPopupCoroutine);
        if (fadeOutCoroutine != null)
            StopCoroutine(fadeOutCoroutine);
        
        // Reset visible timer when combo is refreshed
        visibleTimer = 0f;
        
        // Generate new random position and rotation for each combo
        GenerateRandomPositionAndRotation();
        
        // Update text
        comboText.text = $"COMBO {comboCount}";
        multiplierText.text = $"{multiplier:F1}x";
        
        // Update colors based on combo level
        int colorIndex = Mathf.Min(comboCount - 1, comboColors.Length - 1);
        Color comboColor = comboColors[Mathf.Max(0, colorIndex)];
        
        comboText.color = comboColor;
        multiplierText.color = comboColor;
        
        // Update slider colors
        if (comboSlider != null)
        {
            Image sliderFill = comboSlider.fillRect.GetComponent<Image>();
            if (sliderFill != null)
                sliderFill.color = comboColor;
        }
        
        // Start position transition
        positionCoroutine = StartCoroutine(MoveToPosition());
        
        // Trigger popup animation (this will also reset alpha to 1)
        currentPopupCoroutine = StartCoroutine(PopupAnimation());
    }
    
    private void GenerateRandomPositionAndRotation()
    {
        // Random position on right side of screen
        float randomX = Random.Range(rightSidePositionRange.x, rightSidePositionRange.y);
        float randomY = Random.Range(verticalPositionRange.x, verticalPositionRange.y);
        targetPosition = new Vector3(randomX, randomY, 0);
        
        // Random rotation
        float randomRotation = Random.Range(rotationRange.x, rotationRange.y);
        targetRotation = Quaternion.Euler(0, 0, randomRotation);
    }
    
    public void UpdateComboSlider(float progress)
    {
        if (progress == 0f)
        {
            // Only start fade out if we're not in an active combo
            // Check if combo count is 0 (combo reset) or if we're just ending the window
            if (ComboManager.Instance != null && ComboManager.Instance.GetCurrentCombo() == 0)
            {
                if (fadeOutCoroutine != null)
                    StopCoroutine(fadeOutCoroutine);
                fadeOutCoroutine = StartCoroutine(FadeOut());
            }
            return;
        }
        
        // Show slider if we have a combo
        if (comboSlider != null && comboSlider.gameObject.activeSelf)
        {
            if (currentSliderCoroutine != null)
                StopCoroutine(currentSliderCoroutine);
            currentSliderCoroutine = StartCoroutine(SliderAnimation(progress));
        }
    }
    
    private IEnumerator MoveToPosition()
    {
        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;
        float elapsed = 0f;
        
        while (elapsed < positionTransitionSpeed)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / positionTransitionSpeed;
            
            // Smooth transition to new position and rotation
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, t);
            
            yield return null;
        }
        
        // Ensure we reach the exact target
        transform.localPosition = targetPosition;
        transform.localRotation = targetRotation;
    }
    
    private IEnumerator PopupAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = originalScale;
        Vector3 targetScale = originalScale * popupScaleMultiplier;
        
        // Popup
        while (elapsed < popupDuration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (popupDuration * 0.3f);
            float curveValue = popupCurve.Evaluate(t);
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, curveValue);
            
            yield return null;
        }
        
        // Shake effect
        yield return StartCoroutine(ShakeEffect());
        
        // Settle back
        elapsed = 0f;
        while (elapsed < popupDuration * 0.4f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (popupDuration * 0.4f);
            
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
    }
    
    private IEnumerator ShakeEffect()
    {
        float elapsed = 0f;
        float shakeDuration = 0.2f;
        Vector3 originalPosition = transform.localPosition;
        
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float shakeIntensity = shakeCurve.Evaluate(1f - (elapsed / shakeDuration));
            
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f) * shakeIntensity * 5f,
                Random.Range(-1f, 1f) * shakeIntensity * 5f,
                0
            );
            
            transform.localPosition = originalPosition + randomOffset;
            yield return null;
        }
        
        transform.localPosition = originalPosition;
    }
    
    private IEnumerator SliderAnimation(float targetProgress)
    {
        float startProgress = comboSlider.value;
        float elapsed = 0f;
        float duration = 0.1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            comboSlider.value = Mathf.Lerp(startProgress, targetProgress, t);
            yield return null;
        }
        
        comboSlider.value = targetProgress;
    }
    
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        
        // Use unscaledDeltaTime so time slow doesn't affect fade
        while (elapsed < 1f / fadeSpeed)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed * fadeSpeed;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        if (comboSlider != null)
            comboSlider.value = 0f;
        
        fadeOutCoroutine = null;
    }
}
