using UnityEngine;
using UnityEngine.UI;

public class EnemyTargetIndicator : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] Image targetIndicator;
    [SerializeField] Canvas targetCanvas;

    [Header("Settings")]
    [SerializeField] float indicatorSize = 50f;
    [SerializeField] float edgeOffset = 50f;
    [SerializeField] float targetHeightOffset = 1.5f;
    [SerializeField] Color targetColor = Color.red;
    [SerializeField] Color lockColor = Color.yellow;

    private Camera mainCamera;
    private Enemy currentTarget;

    private void Start()
    {
        mainCamera = Camera.main;
        
        if (targetIndicator == null)
        {
            targetIndicator = GetComponent<Image>();
        }

        if (targetIndicator != null)
        {
            targetIndicator.gameObject.SetActive(false);
            targetIndicator.color = targetColor;
        }
    }

    private void Update()
    {
        if (CameraSoftLock.Instance != null)
        {
            Enemy newTarget = CameraSoftLock.Instance.GetCurrentTarget();
            
            // Check if target changed
            if (newTarget != currentTarget)
            {
                currentTarget = newTarget;
                
                if (currentTarget != null)
                {
                    targetIndicator.gameObject.SetActive(true);
                    targetIndicator.color = CameraSoftLock.Instance.IsInCombat() ? lockColor : targetColor;
                }
                else
                {
                    targetIndicator.gameObject.SetActive(false);
                }
            }

            // Update indicator position
            if (currentTarget != null && targetIndicator != null)
            {
                UpdateIndicatorPosition();
            }
        }
        else
        {
            // Hide if no soft lock system
            if (targetIndicator != null)
            {
                targetIndicator.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateIndicatorPosition()
    {
        if (currentTarget == null || mainCamera == null) return;

        // Apply height offset to target enemy's upper body/head
        Vector3 targetPosition = currentTarget.transform.position + Vector3.up * targetHeightOffset;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetPosition);

        // Check if target is on screen
        if (screenPosition.z > 0 && 
            screenPosition.x >= 0 && screenPosition.x <= Screen.width &&
            screenPosition.y >= 0 && screenPosition.y <= Screen.height)
        {
            // Target is on screen - position indicator at target
            targetIndicator.transform.position = screenPosition;
            targetIndicator.rectTransform.sizeDelta = Vector2.one * indicatorSize;
        }
        else
        {
            // Target is off screen - show edge indicator
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            Vector3 toTarget = screenPosition - screenCenter;
            
            // Normalize and clamp to screen edges
            toTarget.z = 0;
            toTarget.Normalize();
            
            // Calculate edge position
            float screenWidth = Screen.width * 0.5f - edgeOffset;
            float screenHeight = Screen.height * 0.5f - edgeOffset;
            
            float angle = Mathf.Atan2(toTarget.y, toTarget.x);
            float tan = Mathf.Tan(angle);
            
            Vector3 edgePosition;
            
            if (Mathf.Abs(toTarget.x) > Mathf.Abs(toTarget.y))
            {
                // Horizontal edge
                edgePosition = new Vector3(
                    Mathf.Sign(toTarget.x) * screenWidth,
                    toTarget.y * screenWidth / Mathf.Abs(toTarget.x),
                    0
                );
            }
            else
            {
                // Vertical edge
                edgePosition = new Vector3(
                    toTarget.x * screenHeight / Mathf.Abs(toTarget.y),
                    Mathf.Sign(toTarget.y) * screenHeight,
                    0
                );
            }
            
            // Convert to screen coordinates
            screenPosition = screenCenter + edgePosition;
            targetIndicator.transform.position = screenPosition;
            
            // Rotate indicator to point to target
            float rotation = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            targetIndicator.transform.rotation = Quaternion.Euler(0, 0, rotation);
            
            // Make indicator smaller when off-screen
            targetIndicator.rectTransform.sizeDelta = Vector2.one * (indicatorSize * 0.7f);
        }
    }
}
