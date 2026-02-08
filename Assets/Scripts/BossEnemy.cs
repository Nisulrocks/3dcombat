using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class BossEnemy : MonoBehaviour
{
    #region FSM State Types
    public enum BossState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Shield,
        Rage,
        Heal,
        Summon,
        Emote,
        ReturnToCenter
    }
    #endregion

    #region Inspector Settings
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 500f;
    [SerializeField] private float currentHealth;
    [SerializeField] private GameObject ragdoll; 

    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float rotationSpeed = 5f;
    #pragma warning disable CS0414
    [SerializeField] private float stoppingDistance = 2f;
    #pragma warning restore CS0414

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float avoidanceDistance = 3f;
    [SerializeField] private float avoidanceAngle = 45f;
    [SerializeField] private float avoidanceStrength = 2f;
    [SerializeField] private int avoidanceRays = 5;
    [SerializeField] private bool showDebugRays = false;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float patrolPointChangeInterval = 3f;
    [SerializeField] private float activationRadius = 15f;
    [SerializeField] private float returnToCenterRadius = 12f;
    [SerializeField] private Transform centerPoint;
    [SerializeField] private float yLevelThreshold = 5f; 

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 1.5f;
    #pragma warning disable CS0414
    [SerializeField] private float damage = 25f;
    #pragma warning restore CS0414
    [SerializeField] private float rageDamage = 75f;

    [Header("Combo Settings")]
    [SerializeField] private float comboTimeWindow = 0.5f;
    [SerializeField] private int maxComboCount = 3;

    [Header("Heal Settings")]
    [SerializeField] private float healThreshold1 = 0.75f; 
    [SerializeField] private float healThreshold2 = 0.5f;  
    [SerializeField] private float healDuration = 3f;
    [SerializeField] private float healPerSecond = 50f;
    [SerializeField] private float healCooldown = 15f;
    [SerializeField] private float shieldChance = 0.3f;

    [Header("Shield Settings")]
    [SerializeField] private float shieldDuration = 3f;
    [SerializeField] private float shieldCooldown = 5f;

    [Header("Rage Mode Settings")]
    [SerializeField, Range(0f, 1f)] private float rageHealthThreshold = 0.3f; 
    [SerializeField, Range(0f, 1f)] private float rageChance = 0.5f; 
    [SerializeField] private float rageDuration = 10f;
    [SerializeField] private float rageCooldown = 30f;
    #pragma warning disable CS0414
    [SerializeField] private float rageDamageMultiplier = 3f;
    #pragma warning restore CS0414
    [SerializeField] private float pushbackForce = 15f;
    [SerializeField] private float pushbackRadius = 5f;

    [Header("Summon Settings")]
    [SerializeField] private GameObject skeletonPrefab; 
    [SerializeField, Range(0f, 1f)] private float summonChance = 0.4f; 
    [SerializeField] private float summonCooldown = 45f;
    [SerializeField] private float summonDuration = 2f; 
    [SerializeField] private int summonCount = 3; 
    [SerializeField] private float summonRadius = 8f; 
    [SerializeField] private float minSummonDistance = 3f; 
    [SerializeField] private GameObject summonVFX;
    [SerializeField] private AudioClip summonSFX;

    [Header("Emote Settings")]
    [SerializeField] private float emoteDuration = 3f;
    [SerializeField] private float emoteCooldown = 10f;

    [Header("Auto-Heal Settings")]
    [SerializeField] private float combatTimeout = 60f;
    [SerializeField] private float autoHealRate = 5f;

    [Header("References")]
    [SerializeField] private GameObject sword;
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private GameObject rageVFXPrefab;
    [SerializeField] private Transform swordHolder;
    [SerializeField] private Transform shieldHolder;

    [Header("VFX")]
    [SerializeField] private GameObject attack1VFX;
    [SerializeField] private GameObject attack2VFX;
    [SerializeField] private GameObject attack3VFX;
    [SerializeField] private GameObject healVFX;
    [SerializeField] private Vector3 healVFXOffset = Vector3.zero;
    [SerializeField] private GameObject hitVFX;

    [Header("Audio")]
    [SerializeField] private AudioClip rageSFX;
    [SerializeField] private AudioClip healSFX;
    [SerializeField] private AudioClip shieldSFX;
    [SerializeField] private AudioClip[] attackSFX; 
    [SerializeField] private AudioClip alertSFX;
    [SerializeField] private AudioClip deathSFX;
    #endregion

    #region Private Variables
    private Animator animator;
    private CharacterController characterController;
    private BossState currentState = BossState.Idle;
    private GameObject player;
    private GameObject currentShield;
    private GameObject currentRageVFX;
    private AudioSource audioSource;
    private bool hasAlerted = false; 
    
    
    public BossState CurrentState => currentState;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsInvincible => isInvincible;
    public bool IsHealing => isHealing;
    public bool IsInRageMode => isInRageMode;
    public GameObject CurrentShield => currentShield;
    public float ShieldCooldownTimer => shieldCooldownTimer;
    public float RageCooldownTimer => rageCooldownTimer;
    public float RageCooldown => rageCooldown;
    public float ShieldCooldown => shieldCooldown;
    public float HealCooldownTimer => healCooldownTimer;
    public float HealCooldown => healCooldown;
    public float SummonCooldownTimer => summonCooldownTimer;
    public float SummonCooldown => summonCooldown;

    
    private int currentCombo = 0;
    private float lastAttackTime;
    private float lastDamageTime;
    private bool isAttacking = false;
    
    
    private BossHUD bossHUD;

    
    private float shieldCooldownTimer = 0f;
    private float rageCooldownTimer = 0f;
    private float rageTimer = 0f;
    private float healCooldownTimer = 0f;
    private float autoHealTimer = 0f;
    private float summonCooldownTimer = 0f;
    private float emoteCooldownTimer = 0f;
    private bool hasHealedAt75 = false;
    private bool hasHealedAt50 = false;
    
    
    private bool isInvincible = false;
    private bool isInRageMode = false;
    private bool isHealing = false;
    private bool isSummoning = false;

    
    private Vector3 currentPatrolTarget;
    private float patrolTimer = 0f;

    
    private float attackCooldownTimer = 0f;
    private bool canDealDamage = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        
        bossHUD = FindFirstObjectByType<BossHUD>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (centerPoint == null)
        {
            
            GameObject centerObj = new GameObject("BossCenter");
            centerObj.transform.position = transform.position;
            centerPoint = centerObj.transform;
        }

        
        SetRandomPatrolTarget();
    }

    private void Update()
    {
        UpdateTimers();
        UpdateState();
        CheckHealThresholds();
        CheckAutoHeal();
    }
    #endregion

    #region Timer Updates
    private void UpdateTimers()
    {
        if (shieldCooldownTimer > 0) shieldCooldownTimer -= Time.deltaTime;
        if (rageCooldownTimer > 0) rageCooldownTimer -= Time.deltaTime;
        if (attackCooldownTimer > 0) attackCooldownTimer -= Time.deltaTime;
        if (healCooldownTimer > 0) healCooldownTimer -= Time.deltaTime;
        if (summonCooldownTimer > 0) summonCooldownTimer -= Time.deltaTime;
        if (emoteCooldownTimer > 0) emoteCooldownTimer -= Time.deltaTime;

        if (isInRageMode)
        {
            rageTimer -= Time.deltaTime;
            if (rageTimer <= 0)
            {
                
                
                EndRageMode();
            }
        }

        
        autoHealTimer += Time.deltaTime;
    }
    #endregion

    #region State Machine
    private bool IsPlayerOnSameLevel()
    {
        if (player == null) return false;
        
        float yDifference = Mathf.Abs(transform.position.y - player.transform.position.y);
        return yDifference <= yLevelThreshold;
    }

    private void UpdateState()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float distanceToCenter = Vector3.Distance(transform.position, centerPoint.position);

        switch (currentState)
        {
            case BossState.Idle:
                HandleIdleState(distanceToPlayer, distanceToCenter);
                break;
            case BossState.Patrol:
                HandlePatrolState(distanceToPlayer, distanceToCenter);
                break;
            case BossState.Chase:
                HandleChaseState(distanceToPlayer, distanceToCenter);
                break;
            case BossState.Attack:
                HandleAttackState(distanceToPlayer);
                break;
            case BossState.Shield:
                HandleShieldState();
                break;
            case BossState.Rage:
                HandleRageState(distanceToPlayer);
                break;
            case BossState.Heal:
                HandleHealState();
                break;
            case BossState.Summon:
                HandleSummonState();
                break;
            case BossState.Emote:
                HandleEmoteState();
                break;
            case BossState.ReturnToCenter:
                HandleReturnToCenterState();
                break;
        }
    }

    private void ChangeState(BossState newState)
    {
        if (currentState == newState) return;
        
        
        OnStateExit(currentState);
        
        currentState = newState;
        
        
        OnStateEnter(currentState);
    }

    private void OnStateEnter(BossState state)
    {
        Debug.Log($"Boss: Entering state {state}");
        switch (state)
        {
            case BossState.Idle:
                animator.SetFloat("speed", 0f);  
                break;
            case BossState.Patrol:
                animator.SetFloat("speed", 1f);  
                break;
            case BossState.Chase:
                animator.SetFloat("speed", 1f);  
                break;
            case BossState.Attack:
                animator.SetFloat("speed", 0f);  
                StartAttack();
                break;
            case BossState.Shield:
                StartShield();
                break;
            case BossState.Rage:
                StartRageMode();
                break;
            case BossState.Heal:
                StartHeal();
                break;
            case BossState.Summon:
                StartSummon();
                break;
            case BossState.Emote:
                StartEmote();
                break;
            case BossState.ReturnToCenter:
                animator.SetFloat("speed", 1f);  
                break;
        }
    }

    private void OnStateExit(BossState state)
    {
        switch (state)
        {
            case BossState.Shield:
                EndShield();
                break;
            case BossState.Rage:
                
                break;
            case BossState.Heal:
                EndHeal();
                break;
            case BossState.Summon:
                EndSummon();
                break;
        }
    }
    #endregion

    #region State Handlers
    private void HandleIdleState(float distanceToPlayer, float distanceToCenter)
    {
        
        if (distanceToPlayer <= activationRadius && distanceToCenter <= patrolRadius && IsPlayerOnSameLevel())
        {
            
            if (!hasAlerted)
            {
                hasAlerted = true;
                if (alertSFX != null && audioSource != null)
                {
                    audioSource.PlayOneShot(alertSFX);
                }
            }
            
            ChangeState(BossState.Chase);
            return;
        }

        
        hasAlerted = false;
        
        
        ChangeState(BossState.Patrol);
    }

    private void HandlePatrolState(float distanceToPlayer, float distanceToCenter)
    {
        
        if (distanceToPlayer <= activationRadius && distanceToCenter <= patrolRadius && IsPlayerOnSameLevel())
        {
            
            if (!hasAlerted)
            {
                hasAlerted = true;
                if (alertSFX != null && audioSource != null)
                {
                    audioSource.PlayOneShot(alertSFX);
                }
            }
            
            ChangeState(BossState.Chase);
            return;
        }

        
        hasAlerted = false;

        
        if (distanceToCenter > patrolRadius)
        {
            ChangeState(BossState.ReturnToCenter);
            return;
        }

        
        MoveTowards(currentPatrolTarget, patrolSpeed);
        RotateTowards(currentPatrolTarget);

        
        if (Vector3.Distance(transform.position, currentPatrolTarget) < 1f)
        {
            SetRandomPatrolTarget();
        }

        
        patrolTimer += Time.deltaTime;
        if (patrolTimer >= patrolPointChangeInterval)
        {
            SetRandomPatrolTarget();
            patrolTimer = 0f;
        }
    }

    private void HandleChaseState(float distanceToPlayer, float distanceToCenter)
    {
        
        if (distanceToCenter > returnToCenterRadius || distanceToPlayer > activationRadius * 1.5f || !IsPlayerOnSameLevel())
        {
            ChangeState(BossState.ReturnToCenter);
            return;
        }

        
        if (distanceToPlayer <= attackRange && attackCooldownTimer <= 0)
        {
            Debug.Log("Boss: Entering Attack State!");
            ChangeState(BossState.Attack);
            return;
        }

        
        if (ShouldUseShield())
        {
            ChangeState(BossState.Shield);
            return;
        }

        
        if (ShouldUseSummon())
        {
            Debug.Log("Boss: Entering Summon State!");
            ChangeState(BossState.Summon);
            return;
        }

        
        if (distanceToPlayer > attackRange)
        {
            MoveTowards(player.transform.position, chaseSpeed);
            animator.SetFloat("speed", 1f); 
        }
        else
        {
            animator.SetFloat("speed", 0f); 
        }
        
        
        RotateTowards(player.transform.position);
    }

    private void HandleAttackState(float distanceToPlayer)
    {
        if (!isAttacking)
        {
            
            attackCooldownTimer = attackCooldown;
            ChangeState(BossState.Chase);
            return;
        }

        
        RotateTowards(player.transform.position);
    }

    private void HandleShieldState()
    {
        
        if (currentShield == null)
        {
            ChangeState(BossState.Chase);
        }
    }

    private void HandleRageState(float distanceToPlayer)
    {
        if (!isInRageMode)
        {
            ChangeState(BossState.Chase);
            return;
        }

        
        if (distanceToPlayer > attackRange)
        {
            MoveTowards(player.transform.position, chaseSpeed * 1.2f);
        }
        RotateTowards(player.transform.position);
    }

    private void HandleHealState()
    {
        if (!isHealing)
        {
            ChangeState(BossState.Chase);
        }
    }

    private void HandleSummonState()
    {
        
        if (!isSummoning)
        {
            ChangeState(BossState.Chase);
        }
    }

    private void HandleEmoteState()
    {
        
        
    }

    private void HandleReturnToCenterState()
    {
        float distanceToCenter = Vector3.Distance(transform.position, centerPoint.position);
        
        if (distanceToCenter < 1f)
        {
            
            hasAlerted = false; 
            SetRandomPatrolTarget();
            ChangeState(BossState.Patrol);
            return;
        }

        MoveTowards(centerPoint.position, patrolSpeed);
        RotateTowards(centerPoint.position);
    }
    #endregion

    #region Movement
    private Vector3 gravityVelocity;

    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position);
        direction.y = 0;

        
        if (direction.magnitude < 0.1f) return;

        direction.Normalize();
        
        
        Vector3 avoidance = ApplyObstacleAvoidance(direction);
        
        
        Vector3 desiredDirection = direction + Vector3.ClampMagnitude(avoidance, avoidanceStrength);
        desiredDirection.y = 0;
        
        if (desiredDirection.magnitude > 0.01f)
        {
            desiredDirection.Normalize();
        }
        else
        {
            desiredDirection = direction; 
        }

        
        if (characterController != null && characterController.isGrounded)
        {
            gravityVelocity.y = 0f;
        }
        else
        {
            gravityVelocity.y += -9.81f * Time.deltaTime;
        }
        
        Vector3 movement = desiredDirection * speed * Time.deltaTime + gravityVelocity * Time.deltaTime;

        if (characterController != null)
        {
            characterController.Move(movement);
        }
        else
        {
            transform.position += desiredDirection * speed * Time.deltaTime;
        }
    }
    
    private Vector3 ApplyObstacleAvoidance(Vector3 desiredDirection)
    {
        Vector3 avoidanceVector = Vector3.zero;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        if (avoidanceRays < 2) avoidanceRays = 2;

        
        for (int i = 0; i < avoidanceRays; i++)
        {
            float angle = -avoidanceAngle / 2 + (avoidanceAngle / (avoidanceRays - 1)) * i;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * desiredDirection;

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, avoidanceDistance, obstacleLayer))
            {
                float distanceFactor = 1 - (hit.distance / avoidanceDistance);
                
                
                Vector3 slideDirection = Vector3.ProjectOnPlane(desiredDirection, hit.normal).normalized;
                
                
                if (slideDirection.magnitude < 0.1f)
                {
                    slideDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
                    
                    if (Vector3.Dot(slideDirection, desiredDirection) < 0)
                    {
                        slideDirection = -slideDirection;
                    }
                }

                avoidanceVector += slideDirection * distanceFactor * avoidanceStrength;

                if (showDebugRays)
                {
                    Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.red);
                    Debug.DrawRay(hit.point, slideDirection * 2f, Color.yellow);
                }
            }
            else
            {
                if (showDebugRays)
                {
                    Debug.DrawRay(rayOrigin, rayDirection * avoidanceDistance, Color.green);
                }
            }
        }

        
        RaycastHit leftHit;
        Vector3 leftDir = Quaternion.Euler(0, -90, 0) * desiredDirection;
        if (Physics.Raycast(rayOrigin, leftDir, out leftHit, avoidanceDistance * 0.6f, obstacleLayer))
        {
            float distanceFactor = 1 - (leftHit.distance / (avoidanceDistance * 0.6f));
            avoidanceVector += -leftDir * distanceFactor * avoidanceStrength * 0.5f;
            if (showDebugRays)
            {
                Debug.DrawRay(rayOrigin, leftDir * leftHit.distance, Color.magenta);
            }
        }

        RaycastHit rightHit;
        Vector3 rightDir = Quaternion.Euler(0, 90, 0) * desiredDirection;
        if (Physics.Raycast(rayOrigin, rightDir, out rightHit, avoidanceDistance * 0.6f, obstacleLayer))
        {
            float distanceFactor = 1 - (rightHit.distance / (avoidanceDistance * 0.6f));
            avoidanceVector += -rightDir * distanceFactor * avoidanceStrength * 0.5f;
            if (showDebugRays)
            {
                Debug.DrawRay(rayOrigin, rightDir * rightHit.distance, Color.magenta);
            }
        }

        return avoidanceVector;
    }

    private void RotateTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void SetRandomPatrolTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        currentPatrolTarget = centerPoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
    #endregion

    #region Attack System
    private void StartAttack()
    {
        if (isAttacking) return;
        
        isAttacking = true;
        
        
        if (Time.time - lastAttackTime < comboTimeWindow && currentCombo < maxComboCount)
        {
            currentCombo++;
        }
        else
        {
            currentCombo = 1;
        }

        lastAttackTime = Time.time;
        
        
        string attackTrigger = $"attack{currentCombo}";
        animator.SetTrigger(attackTrigger);
        
        
        AudioClip attackSFXToPlay = GetAttackSFX(currentCombo);
        if (attackSFXToPlay != null)
        {
            audioSource.PlayOneShot(attackSFXToPlay);
        }

        
        StartCoroutine(AttackCoroutine());
    }

    private IEnumerator AttackCoroutine()
    {
        float timeout = 0f;
        float maxTimeout = 3f; 
        
        
        while (!canDealDamage && timeout < maxTimeout)
        {
            timeout += Time.deltaTime;
            yield return null;
        }
        
        if (timeout >= maxTimeout)
        {
            Debug.LogWarning("Boss: Attack timeout - damage window never opened!");
        }
        
        
        timeout = 0f;
        while (canDealDamage && timeout < maxTimeout)
        {
            timeout += Time.deltaTime;
            yield return null;
        }
        
        if (timeout >= maxTimeout)
        {
            Debug.LogWarning("Boss: Attack timeout - damage window never closed!");
        }
        
        
        isAttacking = false;
        Debug.Log("Boss: Attack finished");
    }

    private bool ShouldUseShield()
    {
        
        if (shieldCooldownTimer > 0) return false;
        if (currentShield != null) return false; 
        if (Random.value > shieldChance) return false;
        
        
        if (isAttacking) return false;
        
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        return distanceToPlayer < attackRange * 1.5f;
    }

    private bool ShouldUseSummon()
    {
        
        if (summonCooldownTimer > 0) return false;
        if (isSummoning) return false; 
        if (isAttacking) return false; 
        if (skeletonPrefab == null) return false; 
        if (Random.value > summonChance) return false; 

        
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        return distanceToPlayer <= activationRadius;
    }
    #endregion

    #region Shield System
    
    public void StartBlock()
    {
        StartShield();
    }

    private void StartShield()
    {
        if (currentShield != null) return;

        isInvincible = true;
        animator.SetTrigger("shield");
        animator.SetBool("isShielding", true);

        
        if (shieldPrefab != null && shieldHolder != null)
        {
            currentShield = Instantiate(shieldPrefab, shieldHolder);
        }

        
        if (shieldSFX != null)
        {
            audioSource.PlayOneShot(shieldSFX);
        }

        
        StartCoroutine(ShieldCoroutine());
    }

    private IEnumerator ShieldCoroutine()
    {
        yield return new WaitForSeconds(shieldDuration);
        
        if (currentState == BossState.Shield)
        {
            EndShield();
            ChangeState(BossState.Chase);
        }
    }

    private void EndShield()
    {
        isInvincible = false;
        animator.SetBool("isShielding", false);
        shieldCooldownTimer = shieldCooldown;

        if (currentShield != null)
        {
            Destroy(currentShield);
            currentShield = null;
        }
    }
    #endregion

    #region Rage Mode
    private void StartRageMode()
    {
        if (isInRageMode || rageCooldownTimer > 0) return;

        isInRageMode = true;
        isInvincible = true;
        rageTimer = rageDuration;
        rageCooldownTimer = rageCooldown;

        animator.SetTrigger("rage");
        animator.SetBool("isRaging", true);

        
        if (rageVFXPrefab != null)
        {
            currentRageVFX = Instantiate(rageVFXPrefab, transform);
        }

        
        DamageText.CreateRageText(transform.position + Vector3.up * 2f);

        
        if (rageSFX != null)
        {
            audioSource.PlayOneShot(rageSFX);
        }

        
        StartCoroutine(RagePushbackCoroutine());
    }

    private IEnumerator RagePushbackCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        
        PushbackPlayerAndDamage();
    }

    private void PushbackPlayerAndDamage()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer > pushbackRadius) return;

        
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 pushDirection = (player.transform.position - transform.position).normalized;
            pushDirection.y = 0.3f;
            playerRb.AddForce(pushDirection * pushbackForce, ForceMode.Impulse);
        }

        
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null && !playerHealth.IsInvincible)
        {
            
            ShieldSystem shieldSystem = player.GetComponent<ShieldSystem>();
            if (shieldSystem != null && shieldSystem.CurrentShield != null)
            {
                
                Debug.Log("Boss rage damage blocked by player shield!");
                
                
                DamageText.CreateDamageText(player.transform.position, 0, 0); 
                
                return; 
            }
            
            float damage = rageDamage;
            playerHealth.TakeDamage(damage);
            DamageText.CreateRageDamageText(player.transform.position, damage);
        }
    }

    private void EndRageMode()
    {
        Debug.Log("Boss: EndRageMode called - setting isRaging to false");
        isInRageMode = false;
        isInvincible = false;
        animator.SetBool("isRaging", false);

        if (currentRageVFX != null)
        {
            Destroy(currentRageVFX);
            currentRageVFX = null;
        }
        Debug.Log("Boss: Rage animation ended");
    }
    #endregion

    #region Heal System
    private void CheckHealThresholds()
    {
        if (isHealing) return;

        float healthPercent = HealthPercentage;

        
        if (healthPercent <= healThreshold1 && !hasHealedAt75 && healCooldownTimer <= 0)
        {
            hasHealedAt75 = true;
            ChangeState(BossState.Heal);
            return;
        }

        
        if (healthPercent <= healThreshold2 && !hasHealedAt50 && healCooldownTimer <= 0)
        {
            hasHealedAt50 = true;
            ChangeState(BossState.Heal);
            return;
        }
    }

    private void StartHeal()
    {
        if (isHealing) return;

        isHealing = true;
        isInvincible = true;
        healCooldownTimer = healCooldown;

        animator.SetTrigger("heal");
        animator.SetBool("isHealing", true);

        
        if (healVFX != null)
        {
            Vector3 vfxPosition = transform.position + healVFXOffset;
            GameObject healEffect = Instantiate(healVFX, vfxPosition, Quaternion.identity);
            healEffect.transform.SetParent(transform);
            Destroy(healEffect, healDuration);
        }

        
        if (healSFX != null)
        {
            audioSource.PlayOneShot(healSFX);
        }

        
        DamageText.CreateHealText(transform.position + Vector3.up * 2f);

        StartCoroutine(HealCoroutine());
    }

    private IEnumerator HealCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < healDuration)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + healPerSecond * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isHealing = false;
        Debug.Log("Boss: Heal finished, waiting for animation event");
        
    }

    private void EndHeal()
    {
        Debug.Log("Boss: EndHeal called - setting isHealing to false");
        isInvincible = false;
        animator.SetBool("isHealing", false);
        Debug.Log("Boss: Heal animation ended");
    }

    private void StartSummon()
    {
        if (isSummoning) return;

        isSummoning = true;
        isInvincible = true;
        summonCooldownTimer = summonCooldown;

        animator.SetTrigger("summon");

        
        if (summonVFX != null)
        {
            GameObject summonEffect = Instantiate(summonVFX, transform.position, Quaternion.identity);
            Destroy(summonEffect, summonDuration);
        }

        
        if (summonSFX != null)
        {
            audioSource.PlayOneShot(summonSFX);
        }

        StartCoroutine(SummonCoroutine());
    }

    private IEnumerator SummonCoroutine()
    {
        
        yield return new WaitForSeconds(0.5f); 

        
        DamageText.CreateSummonWordText(transform.position + Vector3.up * 2f, "SUMMONED");
        yield return new WaitForSeconds(0.3f);

        
        DamageText.CreateSummonWordText(transform.position + Vector3.up * 2.5f, "UNDEAD");
        yield return new WaitForSeconds(0.3f);

        
        DamageText.CreateSummonWordText(transform.position + Vector3.up * 3f, "SKELEARMY!");
        yield return new WaitForSeconds(0.3f);

        
        for (int i = 0; i < summonCount; i++)
        {
            SpawnSingleSkeleton();
            yield return new WaitForSeconds(0.3f); 
        }

        
        isSummoning = false;
        Debug.Log("Boss: Summon spawning finished, waiting for animation event");
    }

    private void SpawnSkeletons()
    {
        if (skeletonPrefab == null)
        {
            Debug.LogWarning("Boss: No skeleton prefab assigned!");
            return;
        }

        for (int i = 0; i < summonCount; i++)
        {
            
            Vector2 randomCircle = Random.insideUnitCircle;
            Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            spawnOffset = spawnOffset.normalized * Random.Range(minSummonDistance, summonRadius);
            Vector3 spawnPosition = transform.position + spawnOffset;

            
            GameObject skeleton = Instantiate(skeletonPrefab, spawnPosition, Quaternion.identity);
            
            Debug.Log($"Boss: Summoned skeleton at {spawnPosition}");
        }
    }

    private void SpawnSingleSkeleton()
    {
        if (skeletonPrefab == null)
        {
            Debug.LogWarning("Boss: No skeleton prefab assigned!");
            return;
        }

        
        Vector2 randomCircle = Random.insideUnitCircle;
        Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
        spawnOffset = spawnOffset.normalized * Random.Range(minSummonDistance, summonRadius);
        Vector3 spawnPosition = transform.position + spawnOffset;

        
        GameObject skeleton = Instantiate(skeletonPrefab, spawnPosition, Quaternion.identity);
        
        
        if (bossHUD != null)
        {
            bossHUD.OnAllySummoned(skeleton);
        }
        
        Debug.Log($"Boss: Summoned single skeleton at {spawnPosition}");
    }

    private void EndSummon()
    {
        isInvincible = false;
        Debug.Log("Boss: Summon animation ended");
    }

    
    public void EndSummonEvent()
    {
        EndSummon();
        if (currentState == BossState.Summon)
        {
            ChangeState(BossState.Chase);
        }
    }
    #endregion

    #region Emote System
    private void StartEmote()
    {
        if (emoteCooldownTimer > 0) return;

        emoteCooldownTimer = emoteCooldown;

        
        int emoteType = Random.Range(0, 3); 
        switch (emoteType)
        {
            case 0:
                animator.SetTrigger("emote_laugh");
                Debug.Log("Boss: Playing LAUGH emote!");
                break;
            case 1:
                animator.SetTrigger("emote_dance");
                Debug.Log("Boss: Playing DANCE emote!");
                break;
            case 2:
                animator.SetTrigger("emote_taunt");
                Debug.Log("Boss: Playing TAUNT emote!");
                break;
        }

        StartCoroutine(EmoteCoroutine());
    }

    private IEnumerator EmoteCoroutine()
    {
        yield return new WaitForSeconds(emoteDuration);
        
        EndEmote();
        if (currentState == BossState.Emote)
        {
            ChangeState(BossState.Patrol);
        }
    }

    private void EndEmote()
    {
        Debug.Log("Boss: Emote animation ended");
        animator.SetFloat("speed", 0f); 
    }

    
    public void EndEmoteEvent()
    {
        EndEmote();
        if (currentState == BossState.Emote)
        {
            
            ChangeState(BossState.Patrol);
        }
    }

    
    public void TriggerVictoryEmote()
    {
        if (emoteCooldownTimer > 0) return;

        ChangeState(BossState.Emote);
        Debug.Log("Boss: Triggered victory emote after killing player!");
    }
    #endregion

    #region Auto-Heal
    private void CheckAutoHeal()
    {
        if (autoHealTimer >= combatTimeout && currentHealth < maxHealth && !isHealing)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + autoHealRate * Time.deltaTime);
        }
    }
    #endregion

    #region Damage System
    public void TakeDamage(float damage)
    {
        if (isInvincible)
        {
            
            if (currentShield != null)
            {
                DamageText.CreateBlockedText(transform.position + Vector3.up);
            }
            else
            {
                DamageText.CreateInvincibleText(transform.position + Vector3.up);
            }
            return;
        }

        currentHealth -= damage;
        autoHealTimer = 0f; 

        
        DamageText.CreateDamageText(transform.position + Vector3.up, damage);

        
        if (hitVFX != null)
        {
            Instantiate(hitVFX, transform.position + Vector3.up, Quaternion.identity);
        }

        
        if (!isAttacking)
        {
            animator.SetTrigger("hit");
        }
        else
        {
            Debug.Log("Boss: Taking damage during attack, skipping hit reaction");
        }

        
        if (HealthPercentage < rageHealthThreshold && !isInRageMode && rageCooldownTimer <= 0)
        {
            if (Random.value <= rageChance)
            {
                Debug.Log($"Boss: Rage triggered! Health: {HealthPercentage:P0}, Chance: {rageChance:P0}");
                ChangeState(BossState.Rage);
                return;
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        
        currentHealth = 0;
        
        
        if (bossHUD != null)
        {
            
            bossHUD.ForceHealthUpdate(0, maxHealth);
            
            
            bossHUD.HideAfterDelay(0.5f);
        }

        
        if (deathSFX != null)
        {
            PlayDeathSoundPersistent(deathSFX);
        }

        
        DestroyBoss();
    }

    private void PlayDeathSoundPersistent(AudioClip clip)
    {
        
        GameObject tempAudioObject = new GameObject("TempDeathSound");
        tempAudioObject.transform.position = transform.position;
        
        
        AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
        
        
        tempAudioSource.clip = clip;
        tempAudioSource.volume = audioSource != null ? audioSource.volume : 1f;
        tempAudioSource.pitch = audioSource != null ? audioSource.pitch : 1f;
        tempAudioSource.spatialBlend = 1f; 
        tempAudioSource.minDistance = 1f;
        tempAudioSource.maxDistance = 50f;
        tempAudioSource.rolloffMode = AudioRolloffMode.Linear;
        
        
        tempAudioSource.Play();
        
        
        Destroy(tempAudioObject, clip.length + 0.1f); 
    }

    private void DestroyBoss()
    {
        
        if (ragdoll != null)
        {
            GameObject spawnedRagdoll = Instantiate(ragdoll, transform.position, transform.rotation);
            
            
            Rigidbody[] ragdollBodies = spawnedRagdoll.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in ragdollBodies)
            {
                rb.AddExplosionForce(5f, transform.position, 5f, 1f, ForceMode.Impulse);
            }
        }

        
        Debug.Log("Boss defeated!");
        
        
        Destroy(gameObject);
    }
    #endregion

    #region Animation Events (Called from Animation)
    
    public void EnableDamageWindow()
    {
        canDealDamage = true;
    }

    
    public void DisableDamageWindow()
    {
        canDealDamage = false;
    }

    
    public void RagePushbackEvent()
    {
        PushbackPlayerAndDamage();
    }

    
    public void EndRageEvent()
    {
        EndRageMode();
        if (currentState == BossState.Rage)
        {
            ChangeState(BossState.Chase);
        }
    }

    
    public void EndHealEvent()
    {
        EndHeal();
        if (currentState == BossState.Heal)
        {
            ChangeState(BossState.Chase);
        }
    }

    
    public AudioClip GetAttackSFX(int comboLevel)
    {
        if (attackSFX == null || attackSFX.Length == 0)
        {
            return null;
        }
        
        
        int index = Mathf.Clamp(comboLevel - 1, 0, attackSFX.Length - 1);
        return attackSFX[index];
    }

    
    public void StartDamageWindow()
    {
        canDealDamage = true; 
        BossDamageDealer damageDealer = GetComponentInChildren<BossDamageDealer>();
        if (damageDealer != null)
        {
            damageDealer.StartDamageWindow();
        }
    }

    public void EndDamageWindow()
    {
        canDealDamage = false; 
        BossDamageDealer damageDealer = GetComponentInChildren<BossDamageDealer>();
        if (damageDealer != null)
        {
            damageDealer.EndDamageWindow();
        }
    }

    public void SetComboLevel(int combo)
    {
        BossDamageDealer damageDealer = GetComponentInChildren<BossDamageDealer>();
        if (damageDealer != null)
        {
            damageDealer.SetComboLevel(combo);
        }
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        
        Gizmos.color = Color.yellow;
        if (centerPoint != null)
        {
            Gizmos.DrawWireSphere(centerPoint.position, patrolRadius);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, patrolRadius);
        }

        
        Gizmos.color = Color.green;
        if (centerPoint != null)
        {
            Gizmos.DrawWireSphere(centerPoint.position, activationRadius);
        }

        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, pushbackRadius);
    }
    #endregion
}
