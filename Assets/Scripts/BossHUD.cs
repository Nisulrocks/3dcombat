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
    [SerializeField] private Color healingColor = new Color(0f, 0.8f, 0.2f, 0.8f); 
    [SerializeField] private Color invincibleColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); 
    
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
        
        if (hudPanel != null)
            hudPanel.SetActive(false);
    }
    
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        
        
        bossEnemy = FindFirstObjectByType<BossEnemy>();
        if (bossEnemy == null)
        {
            Debug.LogWarning("BossHUD: No BossEnemy found in scene!");
            return;
        }
        
        
        InitializeSliders();
        
        
        updateCoroutine = UpdateHUDRoutine();
        StartCoroutine(updateCoroutine);
    }
    
    private void Update()
    {
        
        if (player == null)
        {
            
            if (hudPanel != null && hudPanel.activeSelf)
            {
                hudPanel.SetActive(false);
            }
            StopUpdateCoroutine();
            
            
            player = GameObject.FindGameObjectWithTag("Player");
            return;
        }
        
        
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            
            if (hudPanel != null && hudPanel.activeSelf)
            {
                hudPanel.SetActive(false);
            }
            StopUpdateCoroutine();
            return;
        }
        
        if (bossEnemy == null) return;
        
        
        float distanceToBoss = Vector3.Distance(player.transform.position, bossEnemy.transform.position);
        bool shouldShowHUD = distanceToBoss <= hudShowDistance;
        
        
        if (shouldShowHUD && !isInRange)
        {
            
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
            
            isInRange = false;
            if (hudPanel != null)
                hudPanel.SetActive(false);
            StopUpdateCoroutine();
        }
    }
    
    private void InitializeSliders()
    {
        
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = 1;
            healthSlider.value = 1;
        }
        
        
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
        
        if (player == null)
        {
            
            StopUpdateCoroutine();
            return;
        }
        
        
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null && playerHealth.CurrentHealth <= 0)
        {
            
            StopUpdateCoroutine();
            return;
        }
        
        if (bossEnemy == null) return;
        
        
        UpdateHealthBar();
        
        
        UpdateTimers();
        
        
        UpdateState();
        
        
        UpdateAllyCount();
    }
    
    private void UpdateHealthBar()
    {
        if (healthSlider == null || bossEnemy == null) return;
        
        float healthPercent = bossEnemy.HealthPercentage;
        healthSlider.value = healthPercent;
        
        
        if (healthText != null)
        {
            
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
        
        
        UpdateTimer(shieldCooldownSlider, shieldCooldownText, bossEnemy.ShieldCooldownTimer, bossEnemy.ShieldCooldown, shieldColor);
        
        
        UpdateTimer(rageCooldownSlider, rageCooldownText, bossEnemy.RageCooldownTimer, bossEnemy.RageCooldown, rageColor);
        
        
        UpdateTimer(healCooldownSlider, healCooldownText, bossEnemy.HealCooldownTimer, bossEnemy.HealCooldown, healColor);
        
        
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
        
        
        summonedAllies.RemoveAll(ally => ally == null);
        
        allyCountText.text = $"Allies: {summonedAllies.Count}";
    }
    
    
    public void OnAllySummoned(GameObject ally)
    {
        if (ally != null)
        {
            summonedAllies.Add(ally);
        }
    }
    
    
    public void OnAllyDied(GameObject ally)
    {
        if (ally != null)
        {
            summonedAllies.Remove(ally);
        }
    }
    
    
    public void OnPlayerRespawn()
    {
        
        player = GameObject.FindGameObjectWithTag("Player");
        
        
        isInRange = false;
        
        Debug.Log("BossHUD: Player respawn detected, ready to show HUD when in range");
    }
    
    
    public float HudShowDistance => hudShowDistance;
    
    
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
        
        
        Image healthFill = healthSlider.fillRect?.GetComponent<Image>();
        if (healthFill != null)
        {
            healthFill.color = GetHealthColor(currentHealth / maxHealth);
        }
        
        Debug.Log($"BossHUD: Force updated health to {currentHealth}/{maxHealth}");
    }
    
    
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
