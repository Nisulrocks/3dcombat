using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageTextPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] GameObject damageTextPrefab;
    [SerializeField] int poolSize = 20;
    [SerializeField] Canvas worldSpaceCanvas; 
    
    private Queue<DamageText> availableTexts = new Queue<DamageText>();
    private List<DamageText> activeTexts = new List<DamageText>();
    
    private void Awake()
    {
        
        if (damageTextPrefab != null)
        {
            InitializePool();
        }
        else
        {
            
            CreateDefaultPrefab();
            InitializePool();
        }
    }
    
    private void CreateDefaultPrefab()
    {
        
        GameObject prefab = new GameObject("DamageText");
        
        
        TextMeshPro textMesh = prefab.AddComponent<TextMeshPro>();
        textMesh.fontSize = 8;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = Color.white;
        textMesh.fontStyle = FontStyles.Bold;
        
        
        textMesh.enableAutoSizing = false;
        textMesh.overflowMode = TextOverflowModes.Overflow;
        
        
        DamageText damageText = prefab.AddComponent<DamageText>();
        
        
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
        
        
        if (availableTexts.Count > 0)
        {
            damageText = availableTexts.Dequeue();
        }
        else
        {
            
            Transform parent = worldSpaceCanvas != null ? worldSpaceCanvas.transform : transform;
            GameObject obj = Instantiate(damageTextPrefab, parent);
            damageText = obj.GetComponent<DamageText>();
            if (damageText == null)
                damageText = obj.AddComponent<DamageText>();
        }
        
        damageText.gameObject.SetActive(true);
        
        TextMeshPro textMesh = damageText.GetComponent<TextMeshPro>();
        if (textMesh != null)
            textMesh.enabled = true;
            
        TextMeshProUGUI textMeshUGUI = damageText.GetComponent<TextMeshProUGUI>();
        if (textMeshUGUI != null)
            textMeshUGUI.enabled = true;
        
        
        activeTexts.Add(damageText);
        return damageText;
    }
    
    public void ReturnDamageText(DamageText damageText)
    {
        if (damageText != null && activeTexts.Contains(damageText))
        {
            activeTexts.Remove(damageText);
            
            
            damageText.gameObject.SetActive(false);
            
            
            
            
            availableTexts.Enqueue(damageText);
        }
    }
    
    
    private void OnDestroy()
    {
        
        foreach (DamageText text in activeTexts)
        {
            if (text != null)
                Destroy(text.gameObject);
        }
        
        
        while (availableTexts.Count > 0)
        {
            DamageText text = availableTexts.Dequeue();
            if (text != null)
                Destroy(text.gameObject);
        }
    }
}
