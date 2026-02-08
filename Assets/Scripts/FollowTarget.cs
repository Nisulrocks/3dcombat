using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The target object to follow")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("Should follow target's position?")]
    public bool followPosition = true;

    [Tooltip("Should follow target's rotation?")]
    public bool followRotation = true;

    [Tooltip("Smooth follow speed (0 = instant, higher = slower)")]
    [Range(0f, 20f)]
    public float smoothSpeed = 0f;

    [Header("Player Detection")]
    [Tooltip("Automatically find player when target is lost (useful for respawn)")]
    public bool autoFindPlayer = true;

    [Tooltip("How often to check for missing target (seconds)")]
    public float checkInterval = 1f;

    [Tooltip("Player tag to search for")]
    public string playerTag = "Player";

    
    private Vector3 initialPositionOffset;
    private Quaternion initialRotationOffset;
    private bool offsetsCalculated = false;
    private float lastCheckTime;
    
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool originalPositionCached = false;

    void Start()
    {
        
        if (!originalPositionCached)
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalPositionCached = true;
        }
        
        
        if (target != null)
        {
            CalculateOffsets();
        }
    }

    void LateUpdate()
    {
        
        if (target == null)
        {
            if (autoFindPlayer && Time.time - lastCheckTime >= checkInterval)
            {
                TryFindPlayer();
                lastCheckTime = Time.time;
            }
            
            
            if (target == null)
            {
                return;
            }
        }

        
        if (!offsetsCalculated)
        {
            CalculateOffsets();
        }

        
        Vector3 targetPosition = transform.position;
        Quaternion targetRotation = transform.rotation;

        if (followPosition)
        {
            
            targetPosition = target.position + target.rotation * initialPositionOffset;
        }

        if (followRotation)
        {
            
            targetRotation = target.rotation * initialRotationOffset;
        }

        
        if (smoothSpeed > 0f)
        {
            if (followPosition)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
            }
            if (followRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
            }
        }
        else
        {
            
            if (followPosition)
            {
                transform.position = targetPosition;
            }
            if (followRotation)
            {
                transform.rotation = targetRotation;
            }
        }
    }

    
    
    
    private void CalculateOffsets()
    {
        if (target == null) return;

        
        initialPositionOffset = Quaternion.Inverse(target.rotation) * (transform.position - target.position);

        
        initialRotationOffset = Quaternion.Inverse(target.rotation) * transform.rotation;

        offsetsCalculated = true;
    }

    
    
    
    public void RecalculateOffset()
    {
        CalculateOffsets();
    }

    
    
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            CalculateOffsets();
        }
    }
    
    
    
    
    public void ResetToOriginalPosition()
    {
        if (originalPositionCached)
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            Debug.Log($"FollowTarget: Reset to original position {originalPosition}");
        }
        else
        {
            Debug.LogWarning("FollowTarget: Original position not cached, cannot reset");
        }
    }
    
    
    
    
    public void ForceRecalculateOffsets()
    {
        if (target != null)
        {
            CalculateOffsets();
            Debug.Log($"FollowTarget: Force recalculated offsets for target {target.name}");
        }
        else
        {
            Debug.LogWarning("FollowTarget: No target available for offset calculation");
        }
    }
    
    
    
    
    private void TryFindPlayer()
    {
        GameObject player = GameObject.FindWithTag(playerTag);
        if (player != null)
        {
            target = player.transform;
            
            
            if (offsetsCalculated)
            {
                Debug.Log($"FollowTarget: Found new player target: {player.name}, maintaining existing offsets");
            }
            else
            {
                
                CalculateOffsets();
                Debug.Log($"FollowTarget: Found new player target: {player.name}, calculated new offsets");
            }
        }
        else
        {
            Debug.Log($"FollowTarget: Could not find player with tag '{playerTag}'");
        }
    }
}