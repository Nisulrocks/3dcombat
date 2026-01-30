using System.Collections;
using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    [Header("Text Settings")]
    [SerializeField] TextMeshPro textMesh; // For World Space damage text
    [SerializeField] float popupDuration = 2f;
    [SerializeField] float fadeSpeed = 2f;
    
    [Header("Movement")]
    [SerializeField] float upwardForce = 5f;
    [SerializeField] float randomHorizontalRange = 2f;
    [SerializeField] float gravity = -9.81f;
    
    [Header("Visual Effects")]
    [SerializeField] Color[] damageColors = {Color.white, Color.yellow, Color.orange, Color.red};
    [SerializeField] Color blockedColor = Color.cyan; // Color for "BLOCKED" text
    [SerializeField] Color invincibleColor = Color.magenta; // Color for "INVINCIBLE" text
    [SerializeField] Color superColor = new Color(1f, 0.5f, 0f); // Orange color for "SUPER!" text
    [SerializeField] float popupScaleMultiplier = 1.2f; // Much smaller scale
    [SerializeField] AnimationCurve popupCurve;
    
    private Vector3 velocity;
    private float timeAlive;
    private bool isFading;
    private Vector3 originalScale; // Store original scale
    
    public static void CreateDamageText(Vector3 position, float damage, int comboLevel = 0)
    {
        // Find or create damage text pool
        DamageTextPool pool = FindObjectOfType<DamageTextPool>();
        if (pool == null)
        {
            // Create pool if none exists
            GameObject poolObj = new GameObject("DamageTextPool");
            pool = poolObj.AddComponent<DamageTextPool>();
        }
        
        // Get damage text from pool
        DamageText damageText = pool.GetDamageText();
        damageText.transform.position = position;
        damageText.Setup(damage, comboLevel);
    }

    public static void CreateSuperDamageText(Vector3 position, float damage)
    {
        // Find or create damage text pool
        DamageTextPool pool = FindObjectOfType<DamageTextPool>();
        if (pool == null)
        {
            GameObject poolObj = new GameObject("DamageTextPool");
            pool = poolObj.AddComponent<DamageTextPool>();
        }
        
        DamageText damageText = pool.GetDamageText();
        damageText.transform.position = position;
        damageText.SetupSuper(damage);
    }

    public static void CreateInvincibleText(Vector3 position)
    {
        // Find or create damage text pool
        DamageTextPool pool = FindObjectOfType<DamageTextPool>();
        if (pool == null)
        {
            GameObject poolObj = new GameObject("DamageTextPool");
            pool = poolObj.AddComponent<DamageTextPool>();
        }
        
        DamageText damageText = pool.GetDamageText();
        damageText.transform.position = position;
        damageText.SetupInvincible();
    }
    
    public void Setup(float damage, int comboLevel)
    {
        Debug.Log($"DamageText.Setup called - Damage: {damage}, Combo: {comboLevel}");
        
        // Store original scale on first setup
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        // Use the assigned TextMeshPro component
        if (textMesh == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            ReturnToPool();
            return;
        }
        
        Debug.Log($"Found TextMeshPro component: {textMesh.name}, enabled: {textMesh.enabled}");
        
        // Set text
        if (damage == 0)
        {
            textMesh.text = "BLOCKED";
            textMesh.color = blockedColor;
        }
        else
        {
            textMesh.text = $"{damage:F0}";
            // Set color based on combo level
            int colorIndex = Mathf.Min(comboLevel, damageColors.Length - 1);
            textMesh.color = damageColors[colorIndex];
        }
        
        Debug.Log($"Set text to: {textMesh.text}, color: {textMesh.color}");
        
        // Reset values
        timeAlive = 0f;
        isFading = false;
        velocity = Vector3.zero;
        
        // Add random horizontal force
        Vector3 randomHorizontal = new Vector3(
            Random.Range(-randomHorizontalRange, randomHorizontalRange),
            0,
            Random.Range(-randomHorizontalRange, randomHorizontalRange)
        );
        
        // Apply forces
        velocity = Vector3.up * upwardForce + randomHorizontal;
        
        // Start popup animation
        StartCoroutine(PopupAnimation());
        
        // Start lifetime
        StartCoroutine(LifetimeCoroutine());
        
        Debug.Log("DamageText setup complete - animations started");
    }

    public void SetupSuper(float damage)
    {
        Debug.Log($"DamageText.SetupSuper called - Damage: {damage}");
        
        // Store original scale on first setup
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        if (textMesh == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            ReturnToPool();
            return;
        }
        
        // Set SUPER! text with damage
        textMesh.text = $"SUPER!\n{damage:F0}";
        textMesh.color = superColor;
        
        // Reset values
        timeAlive = 0f;
        isFading = false;
        velocity = Vector3.zero;
        
        // Add random horizontal force
        Vector3 randomHorizontal = new Vector3(
            Random.Range(-randomHorizontalRange, randomHorizontalRange),
            0,
            Random.Range(-randomHorizontalRange, randomHorizontalRange)
        );
        
        // Apply forces
        velocity = Vector3.up * upwardForce + randomHorizontal;
        
        // Start popup animation
        StartCoroutine(PopupAnimation());
        
        // Start lifetime
        StartCoroutine(LifetimeCoroutine());
        
        Debug.Log("DamageText SUPER setup complete");
    }

    public void SetupInvincible()
    {
        Debug.Log($"DamageText.SetupInvincible called");
        
        // Store original scale on first setup
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        if (textMesh == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            ReturnToPool();
            return;
        }
        
        // Set INVINCIBLE text
        textMesh.text = "INVINCIBLE";
        textMesh.color = invincibleColor;
        
        // Reset values
        timeAlive = 0f;
        isFading = false;
        velocity = Vector3.zero;
        
        // Add random horizontal force
        Vector3 randomHorizontal = new Vector3(
            Random.Range(-randomHorizontalRange, randomHorizontalRange),
            0,
            Random.Range(-randomHorizontalRange, randomHorizontalRange)
        );
        
        // Apply forces
        velocity = Vector3.up * upwardForce + randomHorizontal;
        
        // Start popup animation
        StartCoroutine(PopupAnimation());
        
        // Start lifetime
        StartCoroutine(LifetimeCoroutine());
        
        Debug.Log("DamageText INVINCIBLE setup complete");
    }
    
    private IEnumerator PopupAnimation()
    {
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = originalScale * popupScaleMultiplier; // Use original scale
        float elapsed = 0f;
        
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.2f;
            float curveValue = popupCurve.Evaluate(t);
            
            transform.localScale = Vector3.Lerp(startScale, targetScale, curveValue);
            yield return null;
        }
        
        // Settle back to original scale
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.1f;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }
    }
    
    private IEnumerator LifetimeCoroutine()
    {
        // Wait for most of the lifetime before fading
        yield return new WaitForSeconds(popupDuration - 0.5f);
        
        // Start fading
        isFading = true;
        
        // Wait for fade to complete
        yield return new WaitForSeconds(0.5f);
        
        // Return to pool
        ReturnToPool();
    }
    
    private void Update()
    {
        if (textMesh == null) return;
        
        if (isFading)
        {
            // Fade out
            Color currentColor = textMesh.color;
            currentColor.a = Mathf.Max(0, currentColor.a - (fadeSpeed * Time.deltaTime));
            textMesh.color = currentColor;
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        
        // Move
        transform.position += velocity * Time.deltaTime;
        
        // Always face the camera
        transform.rotation = Camera.main.transform.rotation;
        
        timeAlive += Time.deltaTime;
    }
    
    private void ReturnToPool()
    {
        DamageTextPool pool = FindObjectOfType<DamageTextPool>();
        if (pool != null)
        {
            pool.ReturnDamageText(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
