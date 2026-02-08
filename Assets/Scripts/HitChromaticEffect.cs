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
            
            
            if (!volumeProfile.TryGet(out chromaticEffect))
            {
                chromaticEffect = volumeProfile.Add<ChromaticAberration>();
            }
            
            
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

        
        if (chromaticCoroutine != null)
        {
            StopCoroutine(chromaticCoroutine);
        }

        
        chromaticCoroutine = StartCoroutine(HitChromaticCoroutine());
    }

    private System.Collections.IEnumerator HitChromaticCoroutine()
    {
        float timer = 0f;

        
        chromaticEffect.intensity.overrideState = true;
        chromaticEffect.intensity.value = 0f;

        while (timer < chromaticDuration)
        {
            timer += Time.deltaTime;
            
            
            float normalizedTime = timer / chromaticDuration;
            float currentIntensity = chromaticCurve.Evaluate(normalizedTime) * maxIntensity;
            
            
            chromaticEffect.intensity.value = currentIntensity;
            
            yield return null;
        }

        
        chromaticEffect.intensity.value = 0f;
        chromaticEffect.intensity.overrideState = false;
        
        chromaticCoroutine = null;
    }

    
    [ContextMenu("Test Hit Chromatic")]
    public void TestHitChromatic()
    {
        TriggerHitChromatic();
    }

    private void OnDestroy()
    {
        
        if (chromaticCoroutine != null)
        {
            StopCoroutine(chromaticCoroutine);
        }
    }
}
