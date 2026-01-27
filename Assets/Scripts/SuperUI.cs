using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SuperUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Slider superBarSlider;
    [SerializeField] Image fillImage;
    [SerializeField] TextMeshProUGUI superReadyText;
    [SerializeField] GameObject superReadyGlow;
    [SerializeField] Slider superTimerSlider;
    [SerializeField] Image timerFillImage;

    [Header("Colors")]
    [SerializeField] Color chargingColor = Color.yellow;
    [SerializeField] Color readyColor = Color.red;
    [SerializeField] Color timerColor = Color.cyan;

    private void Start()
    {
        if (SuperSystem.Instance != null)
        {
            SuperSystem.Instance.OnSuperChargeChanged += HandleChargeChanged;
            SuperSystem.Instance.OnSuperReady += HandleSuperReady;
            SuperSystem.Instance.OnSuperActivated += HandleSuperActivated;
            SuperSystem.Instance.OnSuperEnded += HandleSuperEnded;
            SuperSystem.Instance.OnSuperTimerChanged += HandleTimerChanged;

            // Initialize UI
            HandleChargeChanged(SuperSystem.Instance.CurrentCharge, SuperSystem.Instance.MaxCharge);
        }

        // Hide ready text initially
        if (superReadyText != null)
            superReadyText.gameObject.SetActive(false);
        if (superReadyGlow != null)
            superReadyGlow.SetActive(false);
        
        // Hide timer slider initially
        if (superTimerSlider != null)
            superTimerSlider.gameObject.SetActive(false);
    }

    private void HandleChargeChanged(float current, float max)
    {
        if (superBarSlider != null)
        {
            superBarSlider.value = max > 0 ? current / max : 0f;
        }

        // Only change color to charging if super is NOT ready
        // This prevents the color from reverting to yellow when super is ready
        if (fillImage != null && SuperSystem.Instance != null && !SuperSystem.Instance.IsSuperReady)
        {
            fillImage.color = chargingColor;
        }
    }

    private void HandleSuperReady()
    {
        if (fillImage != null)
        {
            fillImage.color = readyColor;
        }

        if (superReadyText != null)
        {
            superReadyText.gameObject.SetActive(true);
            superReadyText.text = "SUPER READY!";
        }

        if (superReadyGlow != null)
        {
            superReadyGlow.SetActive(true);
        }
    }

    private void HandleSuperActivated()
    {
        if (superReadyText != null)
        {
            superReadyText.text = "SUPER ACTIVE!";
        }

        // Show timer slider
        if (superTimerSlider != null)
        {
            superTimerSlider.gameObject.SetActive(true);
            superTimerSlider.value = 1f;
            if (timerFillImage != null)
            {
                timerFillImage.color = timerColor;
            }
        }
    }

    private void HandleTimerChanged(float current, float max)
    {
        if (superTimerSlider != null)
        {
            superTimerSlider.value = max > 0 ? current / max : 0f;
        }

        // Update text to show remaining time
        if (superReadyText != null && SuperSystem.Instance != null && SuperSystem.Instance.IsSuperActive)
        {
            superReadyText.text = $"SUPER! {current:F1}s";
        }
    }

    private void HandleSuperEnded()
    {
        if (superReadyText != null)
        {
            superReadyText.gameObject.SetActive(false);
        }

        if (superReadyGlow != null)
        {
            superReadyGlow.SetActive(false);
        }

        if (fillImage != null)
        {
            fillImage.color = chargingColor;
        }

        // Hide timer slider
        if (superTimerSlider != null)
        {
            superTimerSlider.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (SuperSystem.Instance != null)
        {
            SuperSystem.Instance.OnSuperChargeChanged -= HandleChargeChanged;
            SuperSystem.Instance.OnSuperReady -= HandleSuperReady;
            SuperSystem.Instance.OnSuperActivated -= HandleSuperActivated;
            SuperSystem.Instance.OnSuperEnded -= HandleSuperEnded;
            SuperSystem.Instance.OnSuperTimerChanged -= HandleTimerChanged;
        }
    }
}
