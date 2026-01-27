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

    // Events for UI
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
        // Spawn the combo UI prefab
        if (comboUIPrefab != null)
        {
            // Find or create a SCREEN SPACE Canvas (not world space)
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            Canvas screenCanvas = null;
            
            // Look for a screen space overlay canvas first
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    screenCanvas = canvas;
                    break;
                }
            }
            
            // If no screen space overlay found, look for screen space camera
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
            
            // Create a new screen space canvas if none exists
            if (screenCanvas == null)
            {
                GameObject canvasObj = new GameObject("ComboUI Canvas");
                screenCanvas = canvasObj.AddComponent<Canvas>();
                screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            // Spawn the UI as a child of the SCREEN SPACE canvas
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

        // Cancel existing reset coroutine
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }

        // Trigger UI event
        OnComboChanged?.Invoke(currentCombo, GetDamageMultiplier());

        Debug.Log($"Combo: {currentCombo} | Damage Multiplier: {GetDamageMultiplier()}x");
    }

    // Call this when attack animation starts
    public void StartComboWindow(float animationDuration)
    {
        comboWindowActive = true;
        
        // Cancel existing reset coroutine
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
        }

        Debug.Log($"ComboManager: Starting combo window - Duration: {animationDuration:F2}s, Current Combo: {currentCombo}");

        // Start new reset coroutine based on animation duration
        comboResetCoroutine = StartCoroutine(ComboWindowCoroutine(animationDuration));
    }

    // Call this when attack animation ends
    public void EndComboWindow()
    {
        comboWindowActive = false;
        
        // Stop the coroutine if it's running
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = null;
        }
        
        Debug.Log("ComboManager: Ending combo window - Hiding slider");
        
        // Hide the slider
        OnComboWindowChanged?.Invoke(0f);
    }

    private IEnumerator ComboWindowCoroutine(float animationDuration)
    {
        float elapsed = 0f;
        
        // Update slider progress during combo window
        while (elapsed < animationDuration && comboWindowActive)
        {
            elapsed += Time.deltaTime;
            float progress = 1f - (elapsed / animationDuration);
            OnComboWindowChanged?.Invoke(progress);
            yield return null;
        }
        
        // Always reset combo window flag when coroutine ends
        comboWindowActive = false;
        
        // If no next hit was registered, reset combo
        if (currentCombo > 0)
        {
            ResetCombo();
        }
        else
        {
            // Just hide the slider if combo is already 0
            OnComboWindowChanged?.Invoke(0f);
        }
    }

    // Legacy method for external reset (like taking damage)
    public void ResetCombo()
    {
        currentCombo = 0;
        comboWindowActive = false;
        if (comboResetCoroutine != null)
        {
            StopCoroutine(comboResetCoroutine);
            comboResetCoroutine = null;
        }
        
        // Trigger UI event
        OnComboChanged?.Invoke(0, 1f);
        OnComboWindowChanged?.Invoke(0f);
        
        Debug.Log("Combo Reset");
    }

    public int GetCurrentCombo()
    {
        return currentCombo;
    }

    // For UI display
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
