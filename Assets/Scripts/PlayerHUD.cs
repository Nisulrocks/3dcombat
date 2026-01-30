using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    public static PlayerHUD Instance { get; private set; }

    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Color healthColor = Color.red;
    [SerializeField] private Color lowHealthColor = Color.yellow;
    [SerializeField] private float lowHealthThreshold = 0.3f;

    [Header("Stamina UI")]
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Image staminaFillImage;
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private Color staminaColor = Color.green;
    [SerializeField] private Color lowStaminaColor = Color.yellow;
    [SerializeField] private Color noStaminaColor = Color.gray;
    [SerializeField] private float lowStaminaThreshold = 0.3f;

    [Header("Animation Settings")]
    [SerializeField] private float smoothTime = 0.2f;

    private HealthSystem currentHealthSystem;
    private Character currentCharacter;
    private float currentHealthVelocity;
    private float currentStaminaVelocity;
    private float displayedHealth;
    private float displayedStamina;

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
        InitializeHUD();
    }

    private void Update()
    {
        // Check if player references changed (respawn)
        CheckPlayerReferences();
        
        // Update displayed values with smooth animation
        UpdateSmoothValues();
    }

    private void CheckPlayerReferences()
    {
        Character character = FindObjectOfType<Character>();
        if (character != currentCharacter)
        {
            currentCharacter = character;
            currentHealthSystem = character?.GetComponent<HealthSystem>();
            
            // Reset displayed values
            if (currentHealthSystem != null)
            {
                displayedHealth = currentHealthSystem.CurrentHealth;
                displayedStamina = currentCharacter.GetCurrentStamina();
            }
        }
    }

    private void InitializeHUD()
    {
        CheckPlayerReferences();
        
        // Initialize sliders
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = 1;
            healthSlider.value = 1;
        }

        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0;
            staminaSlider.maxValue = 1;
            staminaSlider.value = 1;
        }

        // Set initial colors
        if (healthFillImage != null)
        {
            healthFillImage.color = healthColor;
        }

        if (staminaFillImage != null)
        {
            staminaFillImage.color = staminaColor;
        }
    }

    private void UpdateSmoothValues()
    {
        if (currentHealthSystem == null || currentCharacter == null) return;

        // Smooth health animation
        float targetHealth = currentHealthSystem.CurrentHealth / currentHealthSystem.MaxHealth;
        displayedHealth = Mathf.SmoothDamp(displayedHealth, targetHealth, ref currentHealthVelocity, smoothTime);
        
        // Smooth stamina animation
        float targetStamina = currentCharacter.GetStaminaPercentage();
        displayedStamina = Mathf.SmoothDamp(displayedStamina, targetStamina, ref currentStaminaVelocity, smoothTime);

        // Update UI
        UpdateHealthUI(displayedHealth);
        UpdateStaminaUI(displayedStamina);
    }

    private void UpdateHealthUI(float healthPercentage)
    {
        if (healthSlider != null)
        {
            healthSlider.value = healthPercentage;
        }

        if (healthFillImage != null)
        {
            // Change color based on health level
            if (healthPercentage <= lowHealthThreshold)
            {
                healthFillImage.color = lowHealthColor;
            }
            else
            {
                healthFillImage.color = healthColor;
            }
        }

        if (healthText != null && currentHealthSystem != null)
        {
            healthText.text = $"{Mathf.Round(currentHealthSystem.CurrentHealth)}/{Mathf.Round(currentHealthSystem.MaxHealth)}";
        }
    }

    private void UpdateStaminaUI(float staminaPercentage)
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = staminaPercentage;
        }

        if (staminaFillImage != null)
        {
            // Change color based on stamina level
            if (staminaPercentage <= 0.1f)
            {
                staminaFillImage.color = noStaminaColor;
            }
            else if (staminaPercentage <= lowStaminaThreshold)
            {
                staminaFillImage.color = lowStaminaColor;
            }
            else
            {
                staminaFillImage.color = staminaColor;
            }
        }

        if (staminaText != null && currentCharacter != null)
        {
            float currentStamina = currentCharacter.GetCurrentStamina();
            float maxStamina = currentCharacter.GetMaxStamina();
            staminaText.text = $"{Mathf.Round(currentStamina)}/{Mathf.Round(maxStamina)}";
        }
    }

    public void ResetHUD()
    {
        // Reset displayed values to max
        if (currentHealthSystem != null)
        {
            displayedHealth = currentHealthSystem.MaxHealth;
        }
        else
        {
            displayedHealth = 1f;
        }

        if (currentCharacter != null)
        {
            displayedStamina = currentCharacter.GetMaxStamina();
        }
        else
        {
            displayedStamina = 1f;
        }

        currentHealthVelocity = 0f;
        currentStaminaVelocity = 0f;
    }

    public void ForceHealthUpdate(float currentHealth, float maxHealth)
    {
        // Force immediate health update without smoothing
        float healthPercentage = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        displayedHealth = healthPercentage;
        currentHealthVelocity = 0f;
        
        // Update UI immediately
        UpdateHealthUI(healthPercentage);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
