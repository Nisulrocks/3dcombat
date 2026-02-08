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
    [SerializeField] float bossHeightOffset = 3f; 
    [SerializeField] Color targetColor = Color.red;
    [SerializeField] Color lockColor = Color.yellow;
    [SerializeField] Color bossColor = Color.magenta; 

    private Camera mainCamera;
    private Enemy currentTarget;
    private BossEnemy currentBossTarget;

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
            BossEnemy newBossTarget = CameraSoftLock.Instance.GetCurrentBossTarget();
            
            
            if (newTarget != currentTarget || newBossTarget != currentBossTarget)
            {
                
                if (currentTarget != null)
                {
                    currentTarget.OnDied -= HandleTargetDied;
                }
                
                currentTarget = newTarget;
                currentBossTarget = newBossTarget;
                
                if (currentTarget != null)
                {
                    targetIndicator.gameObject.SetActive(true);
                    targetIndicator.color = CameraSoftLock.Instance.IsInCombat() ? lockColor : targetColor;
                    
                    currentTarget.OnDied += HandleTargetDied;
                }
                else if (currentBossTarget != null)
                {
                    targetIndicator.gameObject.SetActive(true);
                    targetIndicator.color = bossColor; 
                }
                else
                {
                    targetIndicator.gameObject.SetActive(false);
                }
            }

            
            if ((currentTarget != null || currentBossTarget != null) && targetIndicator != null)
            {
                UpdateIndicatorPosition();
            }
        }
        else
        {
            
            if (targetIndicator != null)
            {
                targetIndicator.gameObject.SetActive(false);
            }
        }
    }

    private void HandleTargetDied()
    {
        
        currentTarget = null;
        currentBossTarget = null;
        if (targetIndicator != null)
        {
            targetIndicator.gameObject.SetActive(false);
        }
    }

    private void UpdateIndicatorPosition()
    {
        if (currentTarget == null && currentBossTarget == null || mainCamera == null) return;

        
        Transform targetTransform = currentTarget != null ? currentTarget.transform : currentBossTarget.transform;
        float heightOffset = currentTarget != null ? targetHeightOffset : bossHeightOffset;

        
        Vector3 targetPosition = targetTransform.position + Vector3.up * heightOffset;
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetPosition);

        
        if (screenPosition.z > 0 && 
            screenPosition.x >= 0 && screenPosition.x <= Screen.width &&
            screenPosition.y >= 0 && screenPosition.y <= Screen.height)
        {
            
            targetIndicator.transform.position = screenPosition;
            targetIndicator.rectTransform.sizeDelta = Vector2.one * indicatorSize;
        }
        else
        {
            
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            Vector3 toTarget = screenPosition - screenCenter;
            
            
            toTarget.z = 0;
            toTarget.Normalize();
            
            
            float screenWidth = Screen.width * 0.5f - edgeOffset;
            float screenHeight = Screen.height * 0.5f - edgeOffset;
            
            float angle = Mathf.Atan2(toTarget.y, toTarget.x);
            float tan = Mathf.Tan(angle);
            
            Vector3 edgePosition;
            
            if (Mathf.Abs(toTarget.x) > Mathf.Abs(toTarget.y))
            {
                
                edgePosition = new Vector3(
                    Mathf.Sign(toTarget.x) * screenWidth,
                    toTarget.y * screenWidth / Mathf.Abs(toTarget.x),
                    0
                );
            }
            else
            {
                
                edgePosition = new Vector3(
                    toTarget.x * screenHeight / Mathf.Abs(toTarget.y),
                    Mathf.Sign(toTarget.y) * screenHeight,
                    0
                );
            }
            
            
            screenPosition = screenCenter + edgePosition;
            targetIndicator.transform.position = screenPosition;
            
            
            float rotation = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            targetIndicator.transform.rotation = Quaternion.Euler(0, 0, rotation);
            
            
            targetIndicator.rectTransform.sizeDelta = Vector2.one * (indicatorSize * 0.7f);
        }
    }

    private void OnDestroy()
    {
        
        if (currentTarget != null)
        {
            currentTarget.OnDied -= HandleTargetDied;
        }
        
        
        currentBossTarget = null;
    }
}
