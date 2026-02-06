using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DamageFlashEffect : MonoBehaviour
{
    public static DamageFlashEffect Instance { get; private set; }

    [Header("Damage Flash Settings")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private float maxIntensity = 1f;
    [SerializeField] private float smoothness = 0.5f;
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private VolumeProfile volumeProfile;
    private Vignette vignetteEffect;
    private ColorAdjustments colorAdjustments;
    private Coroutine flashCoroutine;

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

        // Get global volume if not assigned
        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
        }

        // Get volume profile and effects
        if (globalVolume != null)
        {
            volumeProfile = globalVolume.profile;
            
            // Get or add Vignette effect
            if (!volumeProfile.TryGet(out vignetteEffect))
            {
                vignetteEffect = volumeProfile.Add<Vignette>();
            }
            
            // Get or add Color Adjustments for red tint
            if (!volumeProfile.TryGet(out colorAdjustments))
            {
                colorAdjustments = volumeProfile.Add<ColorAdjustments>();
            }
            
            // Initialize effects to disabled state
            vignetteEffect.intensity.overrideState = false;
            colorAdjustments.colorFilter.overrideState = false;
            
            Debug.Log("DamageFlashEffect: Effects initialized");
        }
        else
        {
            Debug.LogWarning("DamageFlashEffect: Global Volume not found!");
        }
    }

    public void TriggerDamageFlash()
    {
        if (vignetteEffect == null || colorAdjustments == null)
        {
            Debug.LogWarning("DamageFlashEffect: Cannot trigger flash - effects not found!");
            return;
        }

        // Stop any existing flash
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        // Start new flash
        flashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    private System.Collections.IEnumerator DamageFlashCoroutine()
    {
        float timer = 0f;

        // Enable and configure vignette effect
        vignetteEffect.intensity.overrideState = true;
        vignetteEffect.smoothness.overrideState = true;
        vignetteEffect.color.overrideState = true;
        
        vignetteEffect.color.value = Color.red;
        vignetteEffect.smoothness.value = smoothness;
        vignetteEffect.intensity.value = 0f;

        // Enable red color filter
        colorAdjustments.colorFilter.overrideState = true;
        colorAdjustments.colorFilter.value = Color.white; // Start neutral

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            
            // Calculate intensity based on curve
            float normalizedTime = timer / flashDuration;
            float currentIntensity = flashCurve.Evaluate(normalizedTime) * maxIntensity;
            
            // Apply vignette intensity
            vignetteEffect.intensity.value = currentIntensity;
            
            // Apply red color filter
            Color redTint = Color.Lerp(Color.white, Color.red, currentIntensity);
            colorAdjustments.colorFilter.value = redTint;
            
            yield return null;
        }

        // Reset effects
        vignetteEffect.intensity.value = 0f;
        vignetteEffect.intensity.overrideState = false;
        vignetteEffect.smoothness.overrideState = false;
        vignetteEffect.color.overrideState = false;
        
        colorAdjustments.colorFilter.value = Color.white;
        colorAdjustments.colorFilter.overrideState = false;
        
        flashCoroutine = null;
    }

    // Test method - call this from inspector or other scripts
    [ContextMenu("Test Damage Flash")]
    public void TestDamageFlash()
    {
        TriggerDamageFlash();
    }

    private void OnDestroy()
    {
        // Clean up coroutine
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
    }
}