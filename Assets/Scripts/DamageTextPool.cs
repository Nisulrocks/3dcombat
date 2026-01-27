using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageTextPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] GameObject damageTextPrefab;
    [SerializeField] int poolSize = 20;
    [SerializeField] Canvas worldSpaceCanvas; // Optional world space canvas
    
    private Queue<DamageText> availableTexts = new Queue<DamageText>();
    private List<DamageText> activeTexts = new List<DamageText>();
    
    private void Awake()
    {
        // Create pool if prefab is assigned
        if (damageTextPrefab != null)
        {
            InitializePool();
        }
        else
        {
            // Create default prefab if none assigned
            CreateDefaultPrefab();
            InitializePool();
        }
    }
    
    private void CreateDefaultPrefab()
    {
        // Create a default damage text prefab
        GameObject prefab = new GameObject("DamageText");
        
        // Add TextMeshPro component
        TextMeshPro textMesh = prefab.AddComponent<TextMeshPro>();
        textMesh.fontSize = 8;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;
        textMesh.fontStyle = FontStyles.Bold;
        
        // Set up TextMeshPro settings
        textMesh.enableAutoSizing = false;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        
        // Add DamageText component
        DamageText damageText = prefab.AddComponent<DamageText>();
        
        // Save as prefab
        damageTextPrefab = prefab;
    }
    
    private void InitializePool()
    {
        Transform parent = worldSpaceCanvas != null ? worldSpaceCanvas.transform : transform;
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(damageTextPrefab, parent);
            obj.SetActive(false);
            
            DamageText damageText = obj.GetComponent<DamageText>();
            if (damageText == null)
                damageText = obj.AddComponent<DamageText>();
                
            availableTexts.Enqueue(damageText);
        }
    }
    
    public DamageText GetDamageText()
    {
        DamageText damageText = null;
        
        // Try to get from pool
        if (availableTexts.Count > 0)
        {
            damageText = availableTexts.Dequeue();
        }
        else
        {
            // Create new one if pool is empty
            Transform parent = worldSpaceCanvas != null ? worldSpaceCanvas.transform : transform;
            GameObject obj = Instantiate(damageTextPrefab, parent);
            damageText = obj.GetComponent<DamageText>();
            if (damageText == null)
                damageText = obj.AddComponent<DamageText>();
        }
        
        // IMPORTANT: Ensure the GameObject and text components are active
        damageText.gameObject.SetActive(true);
        
        // Also ensure the TextMeshPro component is enabled
        TextMeshPro textMesh = damageText.GetComponent<TextMeshPro>();
        if (textMesh != null)
            textMesh.enabled = true;
            
        TextMeshProUGUI textMeshUGUI = damageText.GetComponent<TextMeshProUGUI>();
        if (textMeshUGUI != null)
            textMeshUGUI.enabled = true;
        
        // Track and return
        activeTexts.Add(damageText);
        return damageText;
    }
    
    public void ReturnDamageText(DamageText damageText)
    {
        if (damageText != null && activeTexts.Contains(damageText))
        {
            activeTexts.Remove(damageText);
            
            // Only disable the GameObject, keep TextMeshPro components enabled
            damageText.gameObject.SetActive(false);
            
            // Don't disable the TextMeshPro components - keep them ready for next use
            // This ensures they're immediately usable when reactivated
            
            availableTexts.Enqueue(damageText);
        }
    }
    
    // Optional: Clear pool when scene changes
    private void OnDestroy()
    {
        // Clean up all active texts
        foreach (DamageText text in activeTexts)
        {
            if (text != null)
                Destroy(text.gameObject);
        }
        
        // Clean up pool
        while (availableTexts.Count > 0)
        {
            DamageText text = availableTexts.Dequeue();
            if (text != null)
                Destroy(text.gameObject);
        }
    }
}
