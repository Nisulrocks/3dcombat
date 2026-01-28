using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class CameraSoftLock : MonoBehaviour
{
    public static CameraSoftLock Instance { get; private set; }

    [Header("Soft Lock Settings")]
    [SerializeField] float lockRange = 10f;
    [SerializeField] float lockAngleThreshold = 45f;
    [SerializeField] float influenceStrength = 0.3f;
    [SerializeField] float maxTurnSpeed = 90f;
    [SerializeField] LayerMask enemyLayer;
    [SerializeField] CinemachineCamera freeLookCamera;

    [Header("Visual Feedback")]
    [SerializeField] bool showDebug = true;
    [SerializeField] Color lockColor = Color.red;
    [SerializeField] Color rangeColor = Color.yellow;

    private Enemy currentTarget;
    private List<Enemy> nearbyEnemies = new List<Enemy>();
    private bool inCombatMode = false;

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
        // Find free look camera if not assigned
        if (freeLookCamera == null)
        {
            freeLookCamera = FindObjectOfType<CinemachineCamera>();
        }
    }

    private void Update()
    {
        // Check if we're in combat mode
        inCombatMode = IsInCombatMode();

        if (inCombatMode)
        {
            FindNearbyEnemies();
            UpdateSoftLockTarget();
            ApplySoftLock();
        }
        else
        {
            currentTarget = null;
        }

        // Draw debug lines in Update (Debug.DrawLine works here)
        if (showDebug)
        {
            DrawDebugLines();
        }
    }

    private bool IsInCombatMode()
    {
        // Check if player is in combat state
        Character character = FindObjectOfType<Character>();
        if (character != null)
        {
            return character.movementSM.currentState == character.combatting;
        }
        return false;
    }

    private void FindNearbyEnemies()
    {
        nearbyEnemies.Clear();
        
        // Find all enemies within range
        Collider[] colliders = Physics.OverlapSphere(transform.position, lockRange, enemyLayer);
        
        foreach (Collider collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Check if enemy is in front of player
                Vector3 toEnemy = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, toEnemy);
                
                if (angle <= lockAngleThreshold)
                {
                    nearbyEnemies.Add(enemy);
                }
            }
        }
    }

    private void UpdateSoftLockTarget()
    {
        currentTarget = null;

        if (nearbyEnemies.Count == 0) return;

        // Find the closest enemy to the center of the screen
        float closestAngle = float.MaxValue;
        
        foreach (Enemy enemy in nearbyEnemies)
        {
            Vector3 toEnemy = (enemy.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, toEnemy);
            
            if (angle < closestAngle)
            {
                closestAngle = angle;
                currentTarget = enemy;
            }
        }
    }

    private void ApplySoftLock()
    {
        if (currentTarget == null) return;
        if (freeLookCamera == null) return;

        Character player = FindObjectOfType<Character>();
        if (player == null) return;

        // Get the OrbitalFollow component from the Cinemachine camera
        var orbitalFollow = freeLookCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbitalFollow == null) return;

        // Calculate desired look direction from player to target
        // Use enemy's center/head position instead of their feet
        Vector3 targetPosition = currentTarget.transform.position + Vector3.up * 3.5f; // Adjust height offset as needed
        Vector3 targetDirection = (targetPosition - player.transform.position).normalized;
        
        // Calculate desired horizontal angle (around Y axis)
        float desiredHorizontalAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
        
        // Calculate desired vertical angle (up/down) - FIXED: Remove the negative sign
        float horizontalDistance = new Vector3(targetDirection.x, 0, targetDirection.z).magnitude;
        float desiredVerticalAngle = Mathf.Atan2(targetDirection.y, horizontalDistance) * Mathf.Rad2Deg;

        // Get current orbital angles
        float currentHorizontal = orbitalFollow.HorizontalAxis.Value;
        float currentVertical = orbitalFollow.VerticalAxis.Value;

        // Calculate differences
        float horizontalDiff = Mathf.DeltaAngle(currentHorizontal, desiredHorizontalAngle);
        float verticalDiff = Mathf.DeltaAngle(currentVertical, desiredVerticalAngle);

        // Apply smooth influence
        if (Mathf.Abs(horizontalDiff) > 0.5f)
        {
            float horizontalInfluence = Mathf.Sign(horizontalDiff) * Mathf.Min(
                Mathf.Abs(horizontalDiff) * influenceStrength,
                maxTurnSpeed * Time.deltaTime
            );
            orbitalFollow.HorizontalAxis.Value += horizontalInfluence;
        }

        if (Mathf.Abs(verticalDiff) > 0.5f)
        {
            float verticalInfluence = Mathf.Sign(verticalDiff) * Mathf.Min(
                Mathf.Abs(verticalDiff) * influenceStrength,
                maxTurnSpeed * Time.deltaTime
            );
            orbitalFollow.VerticalAxis.Value += verticalInfluence;
        }
    }

    private void DrawDebugLines()
    {
        // Draw angle threshold
        Vector3 leftDir = Quaternion.Euler(0, -lockAngleThreshold, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, lockAngleThreshold, 0) * transform.forward;
        
        Debug.DrawRay(transform.position, leftDir * lockRange, rangeColor);
        Debug.DrawRay(transform.position, rightDir * lockRange, rangeColor);
        
        // Draw lines to nearby enemies
        foreach (Enemy enemy in nearbyEnemies)
        {
            Debug.DrawLine(transform.position, enemy.transform.position, Color.yellow);
        }
        
        // Draw line to current target
        if (currentTarget != null)
        {
            Debug.DrawLine(transform.position, currentTarget.transform.position, lockColor);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (showDebug)
        {
            // Draw lock range (Gizmos only work here)
            Gizmos.color = rangeColor;
            Gizmos.DrawWireSphere(transform.position, lockRange);
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebug && currentTarget != null)
        {
            // Draw target indicator
            Gizmos.color = lockColor;
            Gizmos.DrawWireSphere(currentTarget.transform.position, 0.5f);
        }
    }

    public Enemy GetCurrentTarget()
    {
        return currentTarget;
    }

    public bool HasTarget()
    {
        return currentTarget != null;
    }

    public bool IsInCombat()
    {
        return inCombatMode;
    }
}
