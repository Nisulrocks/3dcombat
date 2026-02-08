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

        
        if (globalVolume == null)
        {
            globalVolume = FindFirstObjectByType<Volume>();
        }

        
        if (globalVolume != null)
        {
            volumeProfile = globalVolume.profile;
            
            
            if (!volumeProfile.TryGet(out vignetteEffect))
            {
                vignetteEffect = volumeProfile.Add<Vignette>();
            }
            
            
            if (!volumeProfile.TryGet(out colorAdjustments))
            {
                colorAdjustments = volumeProfile.Add<ColorAdjustments>();
            }
            
            
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

        
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        
        flashCoroutine = StartCoroutine(DamageFlashCoroutine());
    }

    private System.Collections.IEnumerator DamageFlashCoroutine()
    {
        float timer = 0f;

        
        vignetteEffect.intensity.overrideState = true;
        vignetteEffect.smoothness.overrideState = true;
        vignetteEffect.color.overrideState = true;
        
        vignetteEffect.color.value = Color.red;
        vignetteEffect.smoothness.value = smoothness;
        vignetteEffect.intensity.value = 0f;

        
        colorAdjustments.colorFilter.overrideState = true;
        colorAdjustments.colorFilter.value = Color.white; 

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            
            
            float normalizedTime = timer / flashDuration;
            float currentIntensity = flashCurve.Evaluate(normalizedTime) * maxIntensity;
            
            
            vignetteEffect.intensity.value = currentIntensity;
            
            
            Color redTint = Color.Lerp(Color.white, Color.red, currentIntensity);
            colorAdjustments.colorFilter.value = redTint;
            
            yield return null;
        }

        
        vignetteEffect.intensity.value = 0f;
        vignetteEffect.intensity.overrideState = false;
        vignetteEffect.smoothness.overrideState = false;
        vignetteEffect.color.overrideState = false;
        
        colorAdjustments.colorFilter.value = Color.white;
        colorAdjustments.colorFilter.overrideState = false;
        
        flashCoroutine = null;
    }

    
    [ContextMenu("Test Damage Flash")]
    public void TestDamageFlash()
    {
        TriggerDamageFlash();
    }

    private void OnDestroy()
    {
        
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
    }
}