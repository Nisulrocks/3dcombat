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
    private BossEnemy currentBossTarget;
    private List<Enemy> nearbyEnemies = new List<Enemy>();
    private List<BossEnemy> nearbyBosses = new List<BossEnemy>();
    private bool inCombatMode = false;
    private Character cachedPlayer;

    private void Awake()
    {
        
        Instance = this;
    }

    private void Start()
    {
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        
        if (freeLookCamera == null)
        {
            freeLookCamera = FindFirstObjectByType<CinemachineCamera>();
        }
        
        
        cachedPlayer = GetComponent<Character>();
        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<Character>();
        }
    }

    public void SetPlayer(Character newPlayer)
    {
        cachedPlayer = newPlayer;
        InitializeReferences();
    }

    public void SetFreeLookCamera(CinemachineCamera camera)
    {
        freeLookCamera = camera;
    }

    private void Update()
    {
        
        inCombatMode = IsInCombatMode();

        if (inCombatMode)
        {
            FindNearbyEnemies();
            FindNearbyBosses();
            UpdateSoftLockTarget();
            ApplySoftLock();
        }
        else
        {
            currentTarget = null;
            currentBossTarget = null;
        }

        
        if (showDebug)
        {
            DrawDebugLines();
        }
    }

    private bool IsInCombatMode()
    {
        
        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<Character>();
        }
        
        if (cachedPlayer != null)
        {
            return cachedPlayer.movementSM.currentState == cachedPlayer.combatting;
        }
        return false;
    }

    private void FindNearbyEnemies()
    {
        nearbyEnemies.Clear();
        
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, lockRange, enemyLayer);
        
        foreach (Collider collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            if (enemy != null && enemy.transform != null)
            {
                
                Vector3 toEnemy = (enemy.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, toEnemy);
                
                if (angle <= lockAngleThreshold)
                {
                    nearbyEnemies.Add(enemy);
                }
            }
        }
    }

    private void FindNearbyBosses()
    {
        nearbyBosses.Clear();
        
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, lockRange, enemyLayer);
        
        foreach (Collider collider in colliders)
        {
            BossEnemy boss = collider.GetComponent<BossEnemy>();
            if (boss != null && boss.transform != null)
            {
                
                Vector3 toBoss = (boss.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, toBoss);
                
                if (angle <= lockAngleThreshold)
                {
                    nearbyBosses.Add(boss);
                }
            }
        }
    }

    private void UpdateSoftLockTarget()
    {
        currentTarget = null;
        currentBossTarget = null;

        
        if (nearbyBosses.Count > 0)
        {
            
            float closestAngle = float.MaxValue;
            
            foreach (BossEnemy boss in nearbyBosses)
            {
                if (boss != null && boss.transform != null)
                {
                    Vector3 toBoss = (boss.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, toBoss);
                    
                    if (angle < closestAngle)
                    {
                        closestAngle = angle;
                        currentBossTarget = boss;
                    }
                }
            }
        }
        else if (nearbyEnemies.Count > 0)
        {
            
            float closestAngle = float.MaxValue;
            
            foreach (Enemy enemy in nearbyEnemies)
            {
                if (enemy != null && enemy.transform != null)
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
        }
    }

    private void ApplySoftLock()
    {
        
        if (currentBossTarget != null)
        {
            ApplySoftLockToTarget(currentBossTarget.transform);
            return;
        }
        
        
        if (currentTarget == null) return;
        if (freeLookCamera == null) return;

        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<Character>();
        }
        if (cachedPlayer == null) return;

        
        var orbitalFollow = freeLookCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbitalFollow == null) return;

        
        
        Vector3 targetPosition = currentTarget.transform.position + Vector3.up * 3.5f; 
        Vector3 targetDirection = (targetPosition - cachedPlayer.transform.position).normalized;
        
        
        float desiredHorizontalAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
        
        
        float horizontalDistance = new Vector3(targetDirection.x, 0, targetDirection.z).magnitude;
        float desiredVerticalAngle = Mathf.Atan2(targetDirection.y, horizontalDistance) * Mathf.Rad2Deg;

        
        float currentHorizontal = orbitalFollow.HorizontalAxis.Value;
        float currentVertical = orbitalFollow.VerticalAxis.Value;

        
        float horizontalDiff = Mathf.DeltaAngle(currentHorizontal, desiredHorizontalAngle);
        float verticalDiff = Mathf.DeltaAngle(currentVertical, desiredVerticalAngle);

        
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

    private void ApplySoftLockToTarget(Transform targetTransform)
    {
        if (targetTransform == null) return;
        if (freeLookCamera == null) return;

        if (cachedPlayer == null)
        {
            cachedPlayer = FindFirstObjectByType<Character>();
        }
        if (cachedPlayer == null) return;

        
        var orbitalFollow = freeLookCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbitalFollow == null) return;

        
        
        Vector3 targetPosition = targetTransform.position + Vector3.up * 4f; 
        Vector3 targetDirection = (targetPosition - cachedPlayer.transform.position).normalized;
        
        
        float desiredHorizontalAngle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
        
        
        float horizontalDistance = new Vector3(targetDirection.x, 0, targetDirection.z).magnitude;
        float desiredVerticalAngle = Mathf.Atan2(targetDirection.y, horizontalDistance) * Mathf.Rad2Deg;

        
        float currentHorizontal = orbitalFollow.HorizontalAxis.Value;
        float currentVertical = orbitalFollow.VerticalAxis.Value;

        
        float horizontalDiff = Mathf.DeltaAngle(currentHorizontal, desiredHorizontalAngle);
        float verticalDiff = Mathf.DeltaAngle(currentVertical, desiredVerticalAngle);

        
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
        
        Vector3 leftDir = Quaternion.Euler(0, -lockAngleThreshold, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, lockAngleThreshold, 0) * transform.forward;
        
        Debug.DrawRay(transform.position, leftDir * lockRange, rangeColor);
        Debug.DrawRay(transform.position, rightDir * lockRange, rangeColor);
        
        
        foreach (Enemy enemy in nearbyEnemies)
        {
            if (enemy != null && enemy.transform != null)
            {
                Debug.DrawLine(transform.position, enemy.transform.position, Color.yellow);
            }
        }
        
        
        foreach (BossEnemy boss in nearbyBosses)
        {
            if (boss != null && boss.transform != null)
            {
                Debug.DrawLine(transform.position, boss.transform.position, Color.magenta);
            }
        }
        
        
        if (currentTarget != null && currentTarget.transform != null)
        {
            Debug.DrawLine(transform.position, currentTarget.transform.position, lockColor);
        }
        
        
        if (currentBossTarget != null && currentBossTarget.transform != null)
        {
            Debug.DrawLine(transform.position, currentBossTarget.transform.position, Color.magenta);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (showDebug)
        {
            
            Gizmos.color = rangeColor;
            Gizmos.DrawWireSphere(transform.position, lockRange);
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebug && currentTarget != null)
        {
            
            Gizmos.color = lockColor;
            Gizmos.DrawWireSphere(currentTarget.transform.position, 0.5f);
        }
    }

    public Enemy GetCurrentTarget()
    {
        return currentTarget;
    }

    public BossEnemy GetCurrentBossTarget()
    {
        return currentBossTarget;
    }

    public bool HasTarget()
    {
        return currentTarget != null || currentBossTarget != null;
    }

    public bool IsInCombat()
    {
        return inCombatMode;
    }
}
