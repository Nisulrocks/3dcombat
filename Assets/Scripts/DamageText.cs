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
    [SerializeField] Color rageColor = new Color(0.8f, 0f, 0f); // Dark red for "RAGE!" text
    [SerializeField] Color rageDamageColor = new Color(1f, 1f, 0f); // Yellow for rage damage
    [SerializeField] Color healColor = new Color(0f, 0.8f, 0.2f); // Green for "HEAL!" text
    [SerializeField] Color summonColor = Color.black; // Black for summon text
    [SerializeField] float popupScaleMultiplier = 1.2f; // Much smaller scale
    #pragma warning disable CS0414
    [SerializeField] float ragePopupScaleMultiplier = 1.8f; // Larger scale for rage damage
    #pragma warning restore CS0414
    [SerializeField] AnimationCurve popupCurve;
    
    private Vector3 velocity;
    private float timeAlive;
    private bool isFading;
    private Vector3 originalScale; // Store original scale
    
    public static void CreateDamageText(Vector3 position, float damage, int comboLevel = 0)
    {
        // Find or create damage text pool
        DamageTextPool pool = FindFirstObjectByType<DamageTextPool>();
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
        DamageTextPool pool = FindFirstObjectByType<DamageTextPool>();
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
        DamageTextPool pool = FindFirstObjectByType<DamageTextPool>();
        if (pool == null)
        {
            GameObject poolObj = new GameObject("DamageTextPool");
            pool = poolObj.AddComponent<DamageTextPool>();
        }
        
        DamageText damageText = pool.GetDamageText();
        damageText.transform.position = position;
        damageText.SetupInvincible();
    }

    public static void CreateRageText(Vector3 position)
    {
        // Find or create damage text pool
        DamageTextPool pool = FindFirstObjectByType<DamageTextPool>();
        if (pool == null)
        {
            GameObject poolObj = new GameObject("DamageTextPool");
            pool = poolObj.AddComponent<DamageTextPool>();
        }
        
        DamageText damageText = pool.GetDamageText();
        damageText.transform.position = position;
        damageText.SetupRage();
    }

    public static void CreateRageDamageText(Vector3 position, float damage)
    {
        // Find or create damage text pool
        DamageTextPool pool = FindFirstObjectByType<DamageTextPool>();
        if (pool == null)
        {
            GameObject poolObj = new GameObject("DamageTextPool");
            pool = poolObj.AddComponent<DamageTextPool>();
        }
        
        DamageText damageText = pool.GetDamageText();
        damageText.transform.position = position;
        damageText.SetupRageDamage(damage);
    }

    public static void CreateBlockedText(Vector3 position)
    {
        // Find or create damage text pool
        DamageTextPool pool = FindFirstObjectByType<DamageTextPool>();
        if (pool == null)
        {
            GameObject poolObj = new GameObject("DamageTextPool");
            pool = poolObj.AddComponent<DamageTextPool>();
        }
        
        DamageText damageText = pool.GetDamageText();
        damageText.transform.position = position;
        damageText.SetupBlocked();
    }

    public static void CreateHealText(Vector3 position)
    {
        // Find or create damage text pool
        DamageTextPool pool = FindFirstObjectByType<DamageTextPool>();
        if (pool == null)
        {
            GameObject poolObj = new GameObject("DamageTextPool");
            pool = poolObj.AddComponent<DamageTextPool>();
        }
        
        DamageText damageText = pool.GetDamageText();
        damageText.transform.position = position;
        damageText.SetupHeal();
    }

    public static void CreateSummonText(Vector3 position)
    {
        // Find or create damage text pool
        DamageTextPool pool = FindFirstObjectByType<DamageTextPool>();
        if (pool == null)
        {
            GameObject poolObj = new GameObject("DamageTextPool");
            pool = poolObj.AddComponent<DamageTextPool>();
        }
        
        DamageText damageText = pool.GetDamageText();
        damageText.transform.position = position;
        damageText.SetupSummon();
    }

    public static void CreateSummonWordText(Vector3 position, string word)
    {
        // Find or create damage text pool
        DamageTextPool pool = FindFirstObjectByType<DamageTextPool>();
        if (pool == null)
        {
            GameObject poolObj = new GameObject("DamageTextPool");
            pool = poolObj.AddComponent<DamageTextPool>();
        }
        
        DamageText damageText = pool.GetDamageText();
        damageText.transform.position = position;
        damageText.SetupSummonWord(word);
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

    public void SetupRage()
    {
        Debug.Log($"DamageText.SetupRage called");
        
        // Store original scale on first setup
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        if (textMesh == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            ReturnToPool();
            return;
        }
        
        // Set RAGE! text
        textMesh.text = "RAGE!";
        textMesh.color = rageColor;
        
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
        
        // Start popup animation with larger scale
        StartCoroutine(PopupAnimation());
        
        // Start lifetime
        StartCoroutine(LifetimeCoroutine());
        
        Debug.Log("DamageText RAGE setup complete");
    }

    public void SetupRageDamage(float damage)
    {
        Debug.Log($"DamageText.SetupRageDamage called - Damage: {damage}");
        
        // Store original scale on first setup
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        if (textMesh == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            ReturnToPool();
            return;
        }
        
        // Set RAGE! text with damage
        textMesh.text = $"RAGE!\n{damage:F0}";
        textMesh.color = rageDamageColor;
        
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
        
        // Start popup animation with rage scale
        StartCoroutine(PopupAnimation());
        
        // Start lifetime
        StartCoroutine(LifetimeCoroutine());
        
        Debug.Log("DamageText RAGE DAMAGE setup complete");
    }

    public void SetupBlocked()
    {
        Debug.Log($"DamageText.SetupBlocked called");
        
        // Store original scale on first setup
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        if (textMesh == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            ReturnToPool();
            return;
        }
        
        // Set BLOCKED text
        textMesh.text = "BLOCKED";
        textMesh.color = blockedColor;
        
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
        
        Debug.Log("DamageText BLOCKED setup complete");
    }

    public void SetupHeal()
    {
        Debug.Log($"DamageText.SetupHeal called");
        
        // Store original scale on first setup
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        if (textMesh == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            ReturnToPool();
            return;
        }
        
        // Set HEAL! text
        textMesh.text = "HEAL!";
        textMesh.color = healColor;
        
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
        
        Debug.Log("DamageText HEAL setup complete");
    }

    public void SetupSummon()
    {
        Debug.Log($"DamageText.SetupSummon called");
        
        // Store original scale on first setup
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        if (textMesh == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            ReturnToPool();
            return;
        }
        
        // Set SUMMONED text (3 lines)
        textMesh.text = "SUMMONED\nUNDEAD\nSKELEARMY!";
        textMesh.color = summonColor;
        
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
        
        Debug.Log("DamageText SUMMON setup complete");
    }

    public void SetupSummonWord(string word)
    {
        Debug.Log($"DamageText.SetupSummonWord called - Word: {word}");
        
        // Store original scale on first setup
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        
        if (textMesh == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            ReturnToPool();
            return;
        }
        
        // Set individual word text
        textMesh.text = word;
        textMesh.color = summonColor;
        
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
        
        Debug.Log($"DamageText SUMMON WORD '{word}' setup complete");
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
        DamageTextPool pool = FindFirstObjectByType<DamageTextPool>();
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
