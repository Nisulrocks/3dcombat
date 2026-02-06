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

    // Private variables to store the initial offset
    private Vector3 initialPositionOffset;
    private Quaternion initialRotationOffset;
    private bool offsetsCalculated = false;
    private float lastCheckTime;
    
    // Cache the original position and rotation for respawn
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool originalPositionCached = false;

    void Start()
    {
        // Cache the original position and rotation
        if (!originalPositionCached)
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalPositionCached = true;
        }
        
        // Calculate offsets when the game starts
        if (target != null)
        {
            CalculateOffsets();
        }
    }

    void LateUpdate()
    {
        // Check if target is lost and try to find player again
        if (target == null)
        {
            if (autoFindPlayer && Time.time - lastCheckTime >= checkInterval)
            {
                TryFindPlayer();
                lastCheckTime = Time.time;
            }
            
            // If still no target after trying, skip this frame
            if (target == null)
            {
                return;
            }
        }

        // Calculate offsets if not done yet (in case target was assigned at runtime)
        if (!offsetsCalculated)
        {
            CalculateOffsets();
        }

        // Calculate the desired position and rotation
        Vector3 targetPosition = transform.position;
        Quaternion targetRotation = transform.rotation;

        if (followPosition)
        {
            // Calculate desired position: target's position + rotated offset
            targetPosition = target.position + target.rotation * initialPositionOffset;
        }

        if (followRotation)
        {
            // Calculate desired rotation: target's rotation * initial rotation offset
            targetRotation = target.rotation * initialRotationOffset;
        }

        // Apply the position and rotation (with optional smoothing)
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
            // Instant follow
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

    /// <summary>
    /// Calculates the initial offset between this object and the target
    /// </summary>
    private void CalculateOffsets()
    {
        if (target == null) return;

        // Calculate position offset in target's local space
        initialPositionOffset = Quaternion.Inverse(target.rotation) * (transform.position - target.position);

        // Calculate rotation offset
        initialRotationOffset = Quaternion.Inverse(target.rotation) * transform.rotation;

        offsetsCalculated = true;
    }

    /// <summary>
    /// Recalculates the offset based on current positions (useful for runtime adjustments)
    /// </summary>
    public void RecalculateOffset()
    {
        CalculateOffsets();
    }

    /// <summary>
    /// Sets a new target and recalculates offsets
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            CalculateOffsets();
        }
    }
    
    /// <summary>
    /// Resets this object to its original cached position and rotation
    /// </summary>
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
    
    /// <summary>
    /// Forces recalculation of offsets from current position (useful after manual position changes)
    /// </summary>
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
    
    /// <summary>
    /// Tries to find the player GameObject when target is lost
    /// </summary>
    private void TryFindPlayer()
    {
        GameObject player = GameObject.FindWithTag(playerTag);
        if (player != null)
        {
            // First, return to original position before setting new target
            if (originalPositionCached)
            {
                transform.position = originalPosition;
                transform.rotation = originalRotation;
            }
            
            target = player.transform;
            offsetsCalculated = false; // Recalculate offsets for new target
            Debug.Log($"FollowTarget: Found new player target: {player.name}, returned to original position");
        }
        else
        {
            Debug.Log($"FollowTarget: Could not find player with tag '{playerTag}'");
        }
    }
}