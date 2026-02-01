using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RespawnUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundPanel;
    [SerializeField] private TextMeshProUGUI respawnText;
    
    [Header("Settings")]
    [SerializeField] private Color panelColor = new Color(0, 0, 0, 0.8f);

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // Setup panel color
        if (backgroundPanel != null)
        {
            backgroundPanel.color = panelColor;
        }
        
        // Hide at start
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        SetAlpha(1f); // Show with full alpha
    }

    public void Hide()
    {
        SetAlpha(0f); // Hide by setting alpha to 0, but keep GameObject active
    }

    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    public float GetAlpha()
    {
        return canvasGroup != null ? canvasGroup.alpha : 1f;
    }

    public void UpdateText(float timeRemaining)
    {
        if (respawnText != null)
        {
            respawnText.text = $"Respawning in {timeRemaining:F1} seconds...";
        }
    }

    public void SetText(string text)
    {
        if (respawnText != null)
        {
            respawnText.text = text;
        }
    }
}
