using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BossHUD : MonoBehaviour
{
    [Header("HUD References")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("Timer Sliders")]
    [SerializeField] private Slider shieldCooldownSlider;
    [SerializeField] private Slider rageCooldownSlider;
    [SerializeField] private Slider healCooldownSlider;
    [SerializeField] private Slider summonCooldownSlider;
    
    [Header("Timer Texts")]
    [SerializeField] private TextMeshProUGUI shieldCooldownText;
    [SerializeField] private TextMeshProUGUI rageCooldownText;
    [SerializeField] private TextMeshProUGUI healCooldownText;
    [SerializeField] private TextMeshProUGUI summonCooldownText;
    
    [Header("Status Displays")]
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI allyCountText;
    
    [Header("Timer Colors")]
    [SerializeField] private Color shieldColor = Color.blue;
    [SerializeField] private Color rageColor = Color.red;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color summonColor = Color.black;
    
    [Header("Health Bar Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private Color healingColor = new Color(0f, 0.8f, 0.2f, 0.8f); // Green with alpha
    [SerializeField] private Color invincibleColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Gray with alpha
    
    [Header("Settings")]
    [SerializeField] private float hudShowDistance = 50f;
    [SerializeField] private float hudUpdateInterval = 0.1f;
    
    private BossEnemy bossEnemy;
    private GameObject player;
    private float nextUpdateTime;
    private List<GameObject> summonedAllies = new List<GameObject>();
    private System.Collections.IEnumerator updateCoroutine;
    private bool isInRange = false;
    
    private void Awake()
    {
        // Hide HUD initially
        if (hudPanel != null)
            hudPanel.SetActive(false);
    }
    
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Find boss enemy
        bossEnemy = FindObjectOfType<BossEnemy>();
        if (bossEnemy == null)
        {
            Debug.LogWarning("BossHUD: No BossEnemy found in scene!");
            return;
        }
        
        // Initialize sliders
        InitializeSliders();
        
        // Start update coroutine
        updateCoroutine = UpdateHUDRoutine();
        StartCoroutine(updateCoroutine);
    }
    
    private void Update()
    {
        // Check if player exists and is alive
        if (player == null)
        {
            // Player is dead/destroyed, hide panel and wait for respawn
            if (hudPanel != null && hudPanel.activeSelf)
            {
                hudPanel.SetActive(false);
            }
            StopUpdateCoroutine();
            
            // Try to find new player (for respawn)
            player = GameObject.FindGameObjectWithTag("Player");
            return;
        }
        
        // Check if player is dead (has HealthSystem)
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            // Player is dead, hide panel
            if (hudPanel != null && hudPanel.activeSelf)
            {
                hudPanel.SetActive(false);
            }
            StopUpdateCoroutine();
            return;
        }
        
        if (bossEnemy == null) return;
        
        // Check if player is in range
        float distanceToBoss = Vector3.Distance(player.transform.position, bossEnemy.transform.position);
        bool shouldShowHUD = distanceToBoss <= hudShowDistance;
        
        // Handle range changes
        if (shouldShowHUD && !isInRange)
        {
            // Entering range
            isInRange = true;
            if (hudPanel != null)
                hudPanel.SetActive(true);
            if (updateCoroutine == null)
            {
                updateCoroutine = UpdateHUDRoutine();
                StartCoroutine(updateCoroutine);
            }
        }
        else if (!shouldShowHUD && isInRange)
        {
            // Leaving range
            isInRange = false;
            if (hudPanel != null)
                hudPanel.SetActive(false);
            StopUpdateCoroutine();
        }
    }
    
    private void InitializeSliders()
    {
        // Health slider
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = 1;
            healthSlider.value = 1;
        }
        
        // Timer sliders (show cooldown progress)
        InitializeTimerSlider(shieldCooldownSlider, shieldColor);
        InitializeTimerSlider(rageCooldownSlider, rageColor);
        InitializeTimerSlider(healCooldownSlider, healColor);
        InitializeTimerSlider(summonCooldownSlider, summonColor);
    }
    
    private void InitializeTimerSlider(Slider slider, Color color)
    {
        if (slider != null)
        {
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = 0;
            
            // Set slider fill color
            Image sliderFill = slider.fillRect?.GetComponent<Image>();
            if (sliderFill != null)
            {
                sliderFill.color = color;
            }
        }
    }
    
    private void StartUpdateCoroutine()
    {
        StopUpdateCoroutine();
        updateCoroutine = UpdateHUDRoutine();
        StartCoroutine(updateCoroutine);
    }
    
    private void StopUpdateCoroutine()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }
    
    private System.Collections.IEnumerator UpdateHUDRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(hudUpdateInterval);
            UpdateHUD();
        }
    }
    
    private void UpdateHUD()
    {
        // Check if player exists and is alive
        if (player == null)
        {
            // Player is dead/destroyed, stop updates
            StopUpdateCoroutine();
            return;
        }
        
        // Check if player is dead (has HealthSystem)
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            // Player is dead, stop updates
            StopUpdateCoroutine();
            return;
        }
        
        if (bossEnemy == null) return;
        
        // Update health bar
        UpdateHealthBar();
        
        // Update timers
        UpdateTimers();
        
        // Update state
        UpdateState();
        
        // Update ally count
        UpdateAllyCount();
    }
    
    private void UpdateHealthBar()
    {
        if (healthSlider == null || bossEnemy == null) return;
        
        float healthPercent = bossEnemy.HealthPercentage;
        healthSlider.value = healthPercent;
        
        // Update health text
        if (healthText != null)
        {
            // Check if boss has active shield
            bool hasShield = bossEnemy.CurrentShield != null;
            
            if (hasShield)
            {
                healthText.text = "SHIELD";
                healthText.color = Color.cyan;
            }
            else
            {
                float currentHealth = bossEnemy.CurrentHealth;
                float maxHealth = bossEnemy.MaxHealth;
                healthText.text = $"Health {currentHealth:F0}/{maxHealth:F0}";
                
                // Set color based on state
                if (bossEnemy.IsInvincible)
                {
                    healthText.color = invincibleColor;
                }
                else if (bossEnemy.IsHealing)
                {
                    healthText.color = healingColor;
                }
                else
                {
                    healthText.color = GetHealthColor(healthPercent);
                }
            }
        }
        
        // Update health bar color
        Image healthFill = healthSlider.fillRect?.GetComponent<Image>();
        if (healthFill != null)
        {
            if (bossEnemy.IsInvincible)
            {
                healthFill.color = invincibleColor;
            }
            else if (bossEnemy.IsHealing)
            {
                healthFill.color = healingColor;
            }
            else
            {
                healthFill.color = GetHealthColor(healthPercent);
            }
        }
    }
    
    private Color GetHealthColor(float healthPercent)
    {
        if (healthPercent > 0.6f)
            return fullHealthColor;
        else if (healthPercent > 0.3f)
            return mediumHealthColor;
        else
            return lowHealthColor;
    }
    
    private void UpdateTimers()
    {
        if (bossEnemy == null) return;
        
        // Shield cooldown
        UpdateTimer(shieldCooldownSlider, shieldCooldownText, bossEnemy.ShieldCooldownTimer, bossEnemy.ShieldCooldown, shieldColor);
        
        // Rage cooldown
        UpdateTimer(rageCooldownSlider, rageCooldownText, bossEnemy.RageCooldownTimer, bossEnemy.RageCooldown, rageColor);
        
        // Heal cooldown
        UpdateTimer(healCooldownSlider, healCooldownText, bossEnemy.HealCooldownTimer, bossEnemy.HealCooldown, healColor);
        
        // Summon cooldown
        UpdateTimer(summonCooldownSlider, summonCooldownText, bossEnemy.SummonCooldownTimer, bossEnemy.SummonCooldown, summonColor);
    }
    
    private void UpdateTimer(Slider slider, TextMeshProUGUI text, float currentTime, float maxTime, Color timerColor)
    {
        if (slider == null || text == null) return;
        
        float progress = maxTime > 0 ? 1 - (currentTime / maxTime) : 1;
        slider.value = progress;
        
        if (currentTime > 0)
        {
            text.text = currentTime.ToString("F1") + "s";
            text.color = timerColor;
        }
        else
        {
            text.text = "READY";
            text.color = Color.green;
        }
    }
    
    private void UpdateState()
    {
        if (stateText == null || bossEnemy == null) return;
        
        string stateName = "Normal";
        Color stateColor = Color.white;
        
        switch (bossEnemy.CurrentState)
        {
            case BossEnemy.BossState.Idle:
                stateName = "Idle";
                stateColor = Color.gray;
                break;
            case BossEnemy.BossState.Patrol:
                stateName = "Patrol";
                stateColor = Color.gray;
                break;
            case BossEnemy.BossState.Chase:
                stateName = "Normal";
                stateColor = Color.white;
                break;
            case BossEnemy.BossState.Attack:
                stateName = "Attacking";
                stateColor = Color.red;
                break;
            case BossEnemy.BossState.Shield:
                stateName = "Shielded";
                stateColor = Color.cyan;
                break;
            case BossEnemy.BossState.Rage:
                stateName = "RAGE";
                stateColor = Color.red;
                break;
            case BossEnemy.BossState.Heal:
                stateName = "Healing";
                stateColor = healingColor;
                break;
            case BossEnemy.BossState.Summon:
                stateName = "Summoning";
                stateColor = Color.magenta;
                break;
            case BossEnemy.BossState.ReturnToCenter:
                stateName = "Returning";
                stateColor = Color.yellow;
                break;
        }
        
        stateText.text = stateName;
        stateText.color = stateColor;
    }
    
    private void UpdateAllyCount()
    {
        if (allyCountText == null) return;
        
        // Clean up dead allies
        summonedAllies.RemoveAll(ally => ally == null);
        
        allyCountText.text = $"Allies: {summonedAllies.Count}";
    }
    
    // Called by BossEnemy when an ally is summoned
    public void OnAllySummoned(GameObject ally)
    {
        if (ally != null)
        {
            summonedAllies.Add(ally);
        }
    }
    
    // Called by BossEnemy when an ally dies
    public void OnAllyDied(GameObject ally)
    {
        if (ally != null)
        {
            summonedAllies.Remove(ally);
        }
    }
    
    // Called by respawn system when player respawns
    public void OnPlayerRespawn()
    {
        // Find new player reference
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Reset range state so HUD can show when player gets close again
        isInRange = false;
        
        Debug.Log("BossHUD: Player respawn detected, ready to show HUD when in range");
    }
    
    // Public properties for BossEnemy to access
    public float HudShowDistance => hudShowDistance;
    
    // Force immediate health update (used when boss dies)
    public void ForceHealthUpdate(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth / maxHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"{Mathf.Round(currentHealth)}/{Mathf.Round(maxHealth)}";
        }
        
        // Update health bar color based on final health
        Image healthFill = healthSlider.fillRect?.GetComponent<Image>();
        if (healthFill != null)
        {
            healthFill.color = GetHealthColor(currentHealth / maxHealth);
        }
        
        Debug.Log($"BossHUD: Force updated health to {currentHealth}/{maxHealth}");
    }
    
    // Hide HUD after delay (used when boss dies)
    public void HideAfterDelay(float delay)
    {
        StartCoroutine(HideAfterDelayCoroutine(delay));
    }
    
    private System.Collections.IEnumerator HideAfterDelayCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (hudPanel != null)
        {
            hudPanel.SetActive(false);
            Debug.Log("BossHUD: Hidden panel after boss death delay");
        }
        
        StopUpdateCoroutine();
    }
}
