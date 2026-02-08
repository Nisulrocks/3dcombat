using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [Header("Combo Settings")]
    [SerializeField] float comboResetTime = 2f;
    [SerializeField] float[] damageMultipliers = {1f, 1.2f, 1.5f, 1.8f, 2f, 2.5f};
    [SerializeField] int maxComboLevel = 5;
    
    [Header("UI")]
    [SerializeField] GameObject comboUIPrefab;

    private int currentCombo = 0;
    private float lastHitTime;
    private Coroutine comboResetCoroutine;
    private bool comboWindowActive = false;

    
    public System.Action<int, float> OnComboChanged;
    public System.Action<float> OnComboWindowChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        
        if (comboUIPrefab != null)
        {
            
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas screenCanvas = null;
            
            
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    screenCanvas = canvas;
                    break;
                }
            }
            
            
            if (screenCanvas == null)
            {
                foreach (Canvas canvas in allCanvases)
                {
                    if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    {
                        screenCanvas = canvas;
                        break;
                    }
                }
            }
            
            
            if (screenCanvas == null)
            {
                GameObject canvasObj = new GameObject("ComboUI Canvas");
                screenCanvas = canvasObj.AddComponent<Canvas>();
                screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            
            GameObject spawnedUI = Instantiate(comboUIPrefab, screenCanvas.transform);
            spawnedUI.SetActive(true);
        }
        else
        {
            Debug.LogWarning("ComboUI prefab not assigned to ComboManager!");
        }
    }

    public float GetDamageMultiplier()
    {
        int comboLevel = Mathf.Min(currentCombo, maxComboLevel);
        return damageMultipliers[comboLevel];
    }

    public void RegisterHit()
    {
        currentCombo++;
        lastHitTime = Time.time;

        
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }

        
        OnComboChanged?.Invoke(currentCombo, GetDamageMultiplier());

        Debug.Log($"Combo: {currentCombo} | Damage Multiplier: {GetDamageMultiplier()}x");
    }

    
    public void StartComboWindow(float animationDuration)
    {
        comboWindowActive = true;
        
        
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }

        Debug.Log($"ComboManager: Starting combo window - Duration: {animationDuration:F2}s, Current Combo: {currentCombo}");

        
        comboResetCoroutine = StartCoroutine(ComboWindowCoroutine(animationDuration));
    }

    
    public void EndComboWindow()
    {
        comboWindowActive = false;
        
        
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = null;
        }
        
        Debug.Log("ComboManager: Ending combo window - Hiding slider");
        
        
        OnComboWindowChanged?.Invoke(0f);
    }

    private IEnumerator ComboWindowCoroutine(float animationDuration)
    {
        float elapsed = 0f;
        
        
        
        while (elapsed < animationDuration && comboWindowActive)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = 1f - (elapsed / animationDuration);
            OnComboWindowChanged?.Invoke(progress);
            yield return null;
        }
        
        
        comboWindowActive = false;
        comboResetCoroutine = null;
        
        
        if (currentCombo > 0)
        {
            ResetCombo();
        }
        else
        {
            
            OnComboWindowChanged?.Invoke(0f);
        }
    }

    
    public void ResetCombo()
    {
        currentCombo = 0;
        comboWindowActive = false;
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = null;
        }
        
        
        OnComboChanged?.Invoke(0, 1f);
        OnComboWindowChanged?.Invoke(0f);
        
        Debug.Log("Combo Reset");
    }

    public int GetCurrentCombo()
    {
        return currentCombo;
    }

    
    public float GetComboProgress()
    {
        if (comboResetCoroutine == null) return 0f;
        
        float timeSinceLastHit = Time.time - lastHitTime;
        return 1f - (timeSinceLastHit / comboResetTime);
    }

    public bool IsComboWindowActive()
    {
        return comboWindowActive;
    }
}
