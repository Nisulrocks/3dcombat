using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HitChromaticEffect : MonoBehaviour
{
    public static HitChromaticEffect Instance { get; private set; }

    [Header("Hit Chromatic Aberration Settings")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private float chromaticDuration = 0.3f;
    [SerializeField] private float maxIntensity = 1f;
    [SerializeField] private AnimationCurve chromaticCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private VolumeProfile volumeProfile;
    private ChromaticAberration chromaticEffect;
    private Coroutine chromaticCoroutine;

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
            
            // Get or add Chromatic Aberration effect
            if (!volumeProfile.TryGet(out chromaticEffect))
            {
                chromaticEffect = volumeProfile.Add<ChromaticAberration>();
            }
            
            // Initialize effect to disabled state
            chromaticEffect.intensity.overrideState = false;
            
            Debug.Log("HitChromaticEffect: Chromatic Aberration effect initialized");
        }
        else
        {
            Debug.LogWarning("HitChromaticEffect: Global Volume not found!");
        }
    }

    public void TriggerHitChromatic()
    {
        if (chromaticEffect == null)
        {
            Debug.LogWarning("HitChromaticEffect: Cannot trigger chromatic - effect not found!");
            return;
        }

        // Stop any existing chromatic effect
        if (chromaticCoroutine != null)
        {
            StopCoroutine(chromaticCoroutine);
        }

        // Start new chromatic effect
        chromaticCoroutine = StartCoroutine(HitChromaticCoroutine());
    }

    private System.Collections.IEnumerator HitChromaticCoroutine()
    {
        float timer = 0f;

        // Enable and configure chromatic aberration effect
        chromaticEffect.intensity.overrideState = true;
        chromaticEffect.intensity.value = 0f;

        while (timer < chromaticDuration)
        {
            timer += Time.deltaTime;
            
            // Calculate intensity based on curve
            float normalizedTime = timer / chromaticDuration;
            float currentIntensity = chromaticCurve.Evaluate(normalizedTime) * maxIntensity;
            
            // Apply chromatic aberration intensity
            chromaticEffect.intensity.value = currentIntensity;
            
            yield return null;
        }

        // Reset effect
        chromaticEffect.intensity.value = 0f;
        chromaticEffect.intensity.overrideState = false;
        
        chromaticCoroutine = null;
    }

    // Test method - call this from inspector or other scripts
    [ContextMenu("Test Hit Chromatic")]
    public void TestHitChromatic()
    {
        TriggerHitChromatic();
    }

    private void OnDestroy()
    {
        // Clean up coroutine
        if (chromaticCoroutine != null)
        {
            StopCoroutine(chromaticCoroutine);
        }
    }
}
