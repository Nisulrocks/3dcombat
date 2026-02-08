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
    [SerializeField] private GameObject ragdoll; // Ragdoll prefab for death

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
    [SerializeField] private float yLevelThreshold = 5f; // Max Y difference for targeting

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
    [SerializeField] private float healThreshold1 = 0.75f; // Heal at 75% health
    [SerializeField] private float healThreshold2 = 0.5f;  // Heal at 50% health
    [SerializeField] private float healDuration = 3f;
    [SerializeField] private float healPerSecond = 50f;
    [SerializeField] private float healCooldown = 15f;
    [SerializeField] private float shieldChance = 0.3f;

    [Header("Shield Settings")]
    [SerializeField] private float shieldDuration = 3f;
    [SerializeField] private float shieldCooldown = 5f;

    [Header("Rage Mode Settings")]
    [SerializeField, Range(0f, 1f)] private float rageHealthThreshold = 0.3f; // Health % to trigger rage
    [SerializeField, Range(0f, 1f)] private float rageChance = 0.5f; // Chance to enter rage when threshold met
    [SerializeField] private float rageDuration = 10f;
    [SerializeField] private float rageCooldown = 30f;
    #pragma warning disable CS0414
    [SerializeField] private float rageDamageMultiplier = 3f;
    #pragma warning restore CS0414
    [SerializeField] private float pushbackForce = 15f;
    [SerializeField] private float pushbackRadius = 5f;

    [Header("Summon Settings")]
    [SerializeField] private GameObject skeletonPrefab; // Enemy prefab to summon
    [SerializeField, Range(0f, 1f)] private float summonChance = 0.4f; // Chance to summon
    [SerializeField] private float summonCooldown = 45f;
    [SerializeField] private float summonDuration = 2f; // Animation duration
    [SerializeField] private int summonCount = 3; // Number of skeletons
    [SerializeField] private float summonRadius = 8f; // Spawn radius around boss
    [SerializeField] private float minSummonDistance = 3f; // Min distance from boss
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
    [SerializeField] private AudioClip[] attackSFX; // Array for different combo levels
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
    private bool hasAlerted = false; // Track if alert sound has been played
    
    // Public properties for HUD access
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

    // Combat tracking
    private int currentCombo = 0;
    private float lastAttackTime;
    private float lastDamageTime;
    private bool isAttacking = false;
    
    // HUD reference
    private BossHUD bossHUD;

    // Cooldowns and timers
    private float shieldCooldownTimer = 0f;
    private float rageCooldownTimer = 0f;
    private float rageTimer = 0f;
    private float healCooldownTimer = 0f;
    private float autoHealTimer = 0f;
    private float summonCooldownTimer = 0f;
    private float emoteCooldownTimer = 0f;
    private bool hasHealedAt75 = false;
    private bool hasHealedAt50 = false;
    
    // State booleans
    private bool isInvincible = false;
    private bool isInRageMode = false;
    private bool isHealing = false;
    private bool isSummoning = false;

    // Patrol
    private Vector3 currentPatrolTarget;
    private float patrolTimer = 0f;

    // Attack
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
        
        // Find HUD reference
        bossHUD = FindFirstObjectByType<BossHUD>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player");
        
        if (centerPoint == null)
        {
            // Create a center point at spawn position
            GameObject centerObj = new GameObject("BossCenter");
            centerObj.transform.position = transform.position;
            centerPoint = centerObj.transform;
        }

        // Set initial patrol target
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
                // Don't change state here - let animation event handle it
                // Just end the rage mode, animation will handle state change
                EndRageMode();
            }
        }

        // Track time since last damage for auto-heal
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
        
        // Exit current state
        OnStateExit(currentState);
        
        currentState = newState;
        
        // Enter new state
        OnStateEnter(currentState);
    }

    private void OnStateEnter(BossState state)
    {
        Debug.Log($"Boss: Entering state {state}");
        switch (state)
        {
            case BossState.Idle:
                animator.SetFloat("speed", 0f);  // Idle at 0
                break;
            case BossState.Patrol:
                animator.SetFloat("speed", 1f);  // Walk at 1
                break;
            case BossState.Chase:
                animator.SetFloat("speed", 1f);  // Walk at 1 (no run anim)
                break;
            case BossState.Attack:
                animator.SetFloat("speed", 0f);  // Idle during attack
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
                animator.SetFloat("speed", 1f);  // Walk at 1
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
                // Rage has its own exit logic
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
        // Check if player is within activation radius AND on same Y level
        if (distanceToPlayer <= activationRadius && distanceToCenter <= patrolRadius && IsPlayerOnSameLevel())
        {
            // Play alert sound on first detection
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

        // Reset alert when player leaves range
        hasAlerted = false;
        
        // Patrol around center if idle for too long
        ChangeState(BossState.Patrol);
    }

    private void HandlePatrolState(float distanceToPlayer, float distanceToCenter)
    {
        // Check if player is within activation radius AND on same Y level
        if (distanceToPlayer <= activationRadius && distanceToCenter <= patrolRadius && IsPlayerOnSameLevel())
        {
            // Play alert sound on first detection
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

        // Reset alert when player leaves range
        hasAlerted = false;

        // Check if too far from center
        if (distanceToCenter > patrolRadius)
        {
            ChangeState(BossState.ReturnToCenter);
            return;
        }

        // Move to patrol target
        MoveTowards(currentPatrolTarget, patrolSpeed);
        RotateTowards(currentPatrolTarget);

        // Check if reached patrol target
        if (Vector3.Distance(transform.position, currentPatrolTarget) < 1f)
        {
            SetRandomPatrolTarget();
        }

        // Change patrol target periodically
        patrolTimer += Time.deltaTime;
        if (patrolTimer >= patrolPointChangeInterval)
        {
            SetRandomPatrolTarget();
            patrolTimer = 0f;
        }
    }

    private void HandleChaseState(float distanceToPlayer, float distanceToCenter)
    {
        // Check if player left the boss area OR is on different Y level
        if (distanceToCenter > returnToCenterRadius || distanceToPlayer > activationRadius * 1.5f || !IsPlayerOnSameLevel())
        {
            ChangeState(BossState.ReturnToCenter);
            return;
        }

        // Check if close enough to attack
        if (distanceToPlayer <= attackRange && attackCooldownTimer <= 0)
        {
            Debug.Log("Boss: Entering Attack State!");
            ChangeState(BossState.Attack);
            return;
        }

        // Check for shield opportunity
        if (ShouldUseShield())
        {
            ChangeState(BossState.Shield);
            return;
        }

        // Check for summon opportunity
        if (ShouldUseSummon())
        {
            Debug.Log("Boss: Entering Summon State!");
            ChangeState(BossState.Summon);
            return;
        }

        // Chase player - but don't walk into them
        if (distanceToPlayer > attackRange)
        {
            MoveTowards(player.transform.position, chaseSpeed);
            animator.SetFloat("speed", 1f); // Walking
        }
        else
        {
            animator.SetFloat("speed", 0f); // Idle when in attack range but cooling down
        }
        
        // Always face player (like Enemy.cs - rotation separate from movement)
        RotateTowards(player.transform.position);
    }

    private void HandleAttackState(float distanceToPlayer)
    {
        if (!isAttacking)
        {
            // Attack finished
            attackCooldownTimer = attackCooldown;
            ChangeState(BossState.Chase);
            return;
        }

        // Continue facing player during attack
        RotateTowards(player.transform.position);
    }

    private void HandleShieldState()
    {
        // Shield state is handled by coroutine
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

        // Chase and attack player during rage
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
        // Summon state is handled by coroutine
        if (!isSummoning)
        {
            ChangeState(BossState.Chase);
        }
    }

    private void HandleEmoteState()
    {
        // Emote state is handled by coroutine
        // Just wait for the emote to finish
    }

    private void HandleReturnToCenterState()
    {
        float distanceToCenter = Vector3.Distance(transform.position, centerPoint.position);
        
        if (distanceToCenter < 1f)
        {
            // Back at center, go to patrol
            hasAlerted = false; // Reset alert when returning to center
            SetRandomPatrolTarget();
            ChangeState(BossState.Patrol);
            return;
        }

        MoveTowards(centerPoint.position, patrolSpeed);
        RotateTowards(centerPoint.position);
    }
    #endregion

    #region Movement
    private void MoveTowards(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;
        
        // Detect obstacles and calculate avoidance
        Vector3 avoidance = ApplyObstacleAvoidance(direction);
        
        // Combine desired direction with obstacle avoidance for movement only
        Vector3 desiredDirection = direction + avoidance;
        desiredDirection.y = 0;
        desiredDirection.Normalize();
        
        if (characterController != null)
        {
            characterController.Move(desiredDirection * speed * Time.deltaTime);
        }
        else
        {
            transform.position += desiredDirection * speed * Time.deltaTime;
        }
    }
    
    // Apply obstacle avoidance to movement direction (following Enemy.cs pattern)
    private Vector3 ApplyObstacleAvoidance(Vector3 desiredDirection)
    {
        Vector3 avoidanceVector = Vector3.zero;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        // Multi-ray obstacle detection in a cone pattern
        for (int i = 0; i < avoidanceRays; i++)
        {
            float angle = -avoidanceAngle / 2 + (avoidanceAngle / (avoidanceRays - 1)) * i;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * desiredDirection;

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, avoidanceDistance, obstacleLayer))
            {
                // Calculate avoidance force based on distance
                float distanceFactor = 1 - (hit.distance / avoidanceDistance);
                
                // Calculate perpendicular avoidance direction (slide along walls)
                Vector3 avoidDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
                
                // Choose direction that moves away from obstacle
                if (Vector3.Dot(avoidDirection, transform.right) < 0)
                {
                    avoidDirection = -avoidDirection;
                }

                avoidanceVector += avoidDirection * distanceFactor * avoidanceStrength;

                if (showDebugRays)
                {
                    Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.red);
                    Debug.DrawRay(hit.point, avoidDirection * 2f, Color.yellow);
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
        
        // Determine which attack to use
        if (Time.time - lastAttackTime < comboTimeWindow && currentCombo < maxComboCount)
        {
            currentCombo++;
        }
        else
        {
            currentCombo = 1;
        }

        lastAttackTime = Time.time;
        
        // Trigger appropriate attack animation
        string attackTrigger = $"attack{currentCombo}";
        animator.SetTrigger(attackTrigger);
        
        // Play attack SFX based on combo level
        AudioClip attackSFXToPlay = GetAttackSFX(currentCombo);
        if (attackSFXToPlay != null)
        {
            audioSource.PlayOneShot(attackSFXToPlay);
        }

        // Start attack coroutine
        StartCoroutine(AttackCoroutine());
    }

    private IEnumerator AttackCoroutine()
    {
        float timeout = 0f;
        float maxTimeout = 3f; // Max 3 seconds for attack
        
        // Wait for damage window to open (controlled by animation events)
        while (!canDealDamage && timeout < maxTimeout)
        {
            timeout += Time.deltaTime;
            yield return null;
        }
        
        if (timeout >= maxTimeout)
        {
            Debug.LogWarning("Boss: Attack timeout - damage window never opened!");
        }
        
        // Damage window is open
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
        
        // Attack finished
        isAttacking = false;
        Debug.Log("Boss: Attack finished");
    }

    private bool ShouldUseShield()
    {
        // Only check shield once when entering attack range, not every frame
        if (shieldCooldownTimer > 0) return false;
        if (currentShield != null) return false; // Already shielding
        if (Random.value > shieldChance) return false;
        
        // Don't shield if already in attack animation
        if (isAttacking) return false;
        
        // Check if player is in attack range
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        return distanceToPlayer < attackRange * 1.5f;
    }

    private bool ShouldUseSummon()
    {
        // Check summon cooldown and chance
        if (summonCooldownTimer > 0) return false;
        if (isSummoning) return false; // Already summoning
        if (isAttacking) return false; // Don't summon during attack
        if (skeletonPrefab == null) return false; // No prefab assigned
        if (Random.value > summonChance) return false; // Random chance check

        // Only summon when player is somewhat close
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        return distanceToPlayer <= activationRadius;
    }
    #endregion

    #region Shield System
    // Called by animation event to START shield (StartBlock for compatibility)
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

        // Spawn shield visual
        if (shieldPrefab != null && shieldHolder != null)
        {
            currentShield = Instantiate(shieldPrefab, shieldHolder);
        }

        // Play shield SFX
        if (shieldSFX != null)
        {
            audioSource.PlayOneShot(shieldSFX);
        }

        // Start shield duration
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

        // Spawn rage VFX
        if (rageVFXPrefab != null)
        {
            currentRageVFX = Instantiate(rageVFXPrefab, transform);
        }

        // Show RAGE! text
        DamageText.CreateRageText(transform.position + Vector3.up * 2f);

        // Play rage SFX
        if (rageSFX != null)
        {
            audioSource.PlayOneShot(rageSFX);
        }

        // Start pushback after short delay (for animation)
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

        // Pushback
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            Vector3 pushDirection = (player.transform.position - transform.position).normalized;
            pushDirection.y = 0.3f;
            playerRb.AddForce(pushDirection * pushbackForce, ForceMode.Impulse);
        }

        // Deal damage
        HealthSystem playerHealth = player.GetComponent<HealthSystem>();
        if (playerHealth != null && !playerHealth.IsInvincible)
        {
            // Check if player has an active shield
            ShieldSystem shieldSystem = player.GetComponent<ShieldSystem>();
            if (shieldSystem != null && shieldSystem.CurrentShield != null)
            {
                // Shield blocked the rage damage!
                Debug.Log("Boss rage damage blocked by player shield!");
                
                // Show "BLOCKED" damage text
                DamageText.CreateDamageText(player.transform.position, 0, 0); // 0 damage, no combo
                
                return; // Don't deal damage
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

        // Heal at 75% threshold
        if (healthPercent <= healThreshold1 && !hasHealedAt75 && healCooldownTimer <= 0)
        {
            hasHealedAt75 = true;
            ChangeState(BossState.Heal);
            return;
        }

        // Heal at 50% threshold
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

        // Spawn heal VFX
        if (healVFX != null)
        {
            Vector3 vfxPosition = transform.position + healVFXOffset;
            GameObject healEffect = Instantiate(healVFX, vfxPosition, Quaternion.identity);
            healEffect.transform.SetParent(transform);
            Destroy(healEffect, healDuration);
        }

        // Play heal SFX
        if (healSFX != null)
        {
            audioSource.PlayOneShot(healSFX);
        }

        // Show HEAL! text
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
        // Don't change state here - let animation event handle it
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

        // Spawn summon VFX
        if (summonVFX != null)
        {
            GameObject summonEffect = Instantiate(summonVFX, transform.position, Quaternion.identity);
            Destroy(summonEffect, summonDuration);
        }

        // Play summon SFX
        if (summonSFX != null)
        {
            audioSource.PlayOneShot(summonSFX);
        }

        StartCoroutine(SummonCoroutine());
    }

    private IEnumerator SummonCoroutine()
    {
        // Wait a bit then start spawning skeletons (controlled by animation events)
        yield return new WaitForSeconds(0.5f); // Give animation time to start

        // Show SUMMONED text first
        DamageText.CreateSummonWordText(transform.position + Vector3.up * 2f, "SUMMONED");
        yield return new WaitForSeconds(0.3f);

        // Show UNDEAD text second
        DamageText.CreateSummonWordText(transform.position + Vector3.up * 2.5f, "UNDEAD");
        yield return new WaitForSeconds(0.3f);

        // Show SKELEARMY! text third
        DamageText.CreateSummonWordText(transform.position + Vector3.up * 3f, "SKELEARMY!");
        yield return new WaitForSeconds(0.3f);

        // Spawn skeletons one by one
        for (int i = 0; i < summonCount; i++)
        {
            SpawnSingleSkeleton();
            yield return new WaitForSeconds(0.3f); // 0.3 seconds between spawns
        }

        // Don't change state here - let animation event handle it
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
            // Random position around boss
            Vector2 randomCircle = Random.insideUnitCircle;
            Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
            spawnOffset = spawnOffset.normalized * Random.Range(minSummonDistance, summonRadius);
            Vector3 spawnPosition = transform.position + spawnOffset;

            // Spawn skeleton
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

        // Random position around boss
        Vector2 randomCircle = Random.insideUnitCircle;
        Vector3 spawnOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
        spawnOffset = spawnOffset.normalized * Random.Range(minSummonDistance, summonRadius);
        Vector3 spawnPosition = transform.position + spawnOffset;

        // Spawn skeleton
        GameObject skeleton = Instantiate(skeletonPrefab, spawnPosition, Quaternion.identity);
        
        // Notify HUD of new ally
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

    // Called by animation event when summon ends
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

        // Play random emote animation
        int emoteType = Random.Range(0, 3); // 3 different emotes
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
        animator.SetFloat("speed", 0f); // Reset speed to idle
    }

    // Called by animation event when emote ends
    public void EndEmoteEvent()
    {
        EndEmote();
        if (currentState == BossState.Emote)
        {
            // Return to patrol after victory emote instead of chase
            ChangeState(BossState.Patrol);
        }
    }

    // Public method to trigger emote (called when boss kills player)
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
            // Show blocked or invincible text
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
        autoHealTimer = 0f; // Reset combat timer

        // Show damage text
        DamageText.CreateDamageText(transform.position + Vector3.up, damage);

        // Hit VFX
        if (hitVFX != null)
        {
            Instantiate(hitVFX, transform.position + Vector3.up, Quaternion.identity);
        }

        // Hit reaction animation (skip if currently attacking to avoid interrupting)
        if (!isAttacking)
        {
            animator.SetTrigger("hit");
        }
        else
        {
            Debug.Log("Boss: Taking damage during attack, skipping hit reaction");
        }

        // Check for rage trigger (configurable health threshold with cooldown and chance)
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
        // Force health to 0 to ensure HUD updates
        currentHealth = 0;
        
        // Update BossHUD immediately before destroying
        if (bossHUD != null)
        {
            // Force immediate update without smoothing
            bossHUD.ForceHealthUpdate(0, maxHealth);
            
            // Hide HUD after 0.5 seconds
            bossHUD.HideAfterDelay(0.5f);
        }

        // Play death sound on temporary AudioSource that persists after destruction
        if (deathSFX != null)
        {
            PlayDeathSoundPersistent(deathSFX);
        }

        // Destroy boss immediately
        DestroyBoss();
    }

    private void PlayDeathSoundPersistent(AudioClip clip)
    {
        // Create a temporary GameObject to hold the AudioSource
        GameObject tempAudioObject = new GameObject("TempDeathSound");
        tempAudioObject.transform.position = transform.position;
        
        // Add AudioSource component
        AudioSource tempAudioSource = tempAudioObject.AddComponent<AudioSource>();
        
        // Configure AudioSource
        tempAudioSource.clip = clip;
        tempAudioSource.volume = audioSource != null ? audioSource.volume : 1f;
        tempAudioSource.pitch = audioSource != null ? audioSource.pitch : 1f;
        tempAudioSource.spatialBlend = 1f; // Make it 3D sound
        tempAudioSource.minDistance = 1f;
        tempAudioSource.maxDistance = 50f;
        tempAudioSource.rolloffMode = AudioRolloffMode.Linear;
        
        // Play the sound
        tempAudioSource.Play();
        
        // Destroy the temporary object after the sound finishes
        Destroy(tempAudioObject, clip.length + 0.1f); // Small buffer to ensure sound completes
    }

    private void DestroyBoss()
    {
        // Spawn ragdoll like regular enemies
        if (ragdoll != null)
        {
            GameObject spawnedRagdoll = Instantiate(ragdoll, transform.position, transform.rotation);
            
            // Transfer velocity to ragdoll if possible
            Rigidbody[] ragdollBodies = spawnedRagdoll.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in ragdollBodies)
            {
                rb.AddExplosionForce(5f, transform.position, 5f, 1f, ForceMode.Impulse);
            }
        }

        // Notify any listeners
        Debug.Log("Boss defeated!");
        
        // Destroy boss
        Destroy(gameObject);
    }
    #endregion

    #region Animation Events (Called from Animation)
    // Called by animation event to enable damage dealing
    public void EnableDamageWindow()
    {
        canDealDamage = true;
    }

    // Called by animation event to disable damage dealing
    public void DisableDamageWindow()
    {
        canDealDamage = false;
    }

    // Called by animation event to start rage pushback
    public void RagePushbackEvent()
    {
        PushbackPlayerAndDamage();
    }

    // Called by animation event to end rage mode (if using animation to control duration)
    public void EndRageEvent()
    {
        EndRageMode();
        if (currentState == BossState.Rage)
        {
            ChangeState(BossState.Chase);
        }
    }

    // Called by animation event when heal ends
    public void EndHealEvent()
    {
        EndHeal();
        if (currentState == BossState.Heal)
        {
            ChangeState(BossState.Chase);
        }
    }

    // Public method to get attack SFX based on combo level
    public AudioClip GetAttackSFX(int comboLevel)
    {
        if (attackSFX == null || attackSFX.Length == 0)
        {
            return null;
        }
        
        // Clamp combo level to valid range (1-based to 0-based index)
        int index = Mathf.Clamp(comboLevel - 1, 0, attackSFX.Length - 1);
        return attackSFX[index];
    }

    // Wrapper methods for Sword Damage Events (since sword can't be dragged to animation events)
    public void StartDamageWindow()
    {
        canDealDamage = true; // Set the flag so AttackCoroutine can proceed
        BossDamageDealer damageDealer = GetComponentInChildren<BossDamageDealer>();
        if (damageDealer != null)
        {
            damageDealer.StartDamageWindow();
        }
    }

    public void EndDamageWindow()
    {
        canDealDamage = false; // Clear the flag so AttackCoroutine can finish
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
        // Patrol radius
        Gizmos.color = Color.yellow;
        if (centerPoint != null)
        {
            Gizmos.DrawWireSphere(centerPoint.position, patrolRadius);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, patrolRadius);
        }

        // Activation radius
        Gizmos.color = Color.green;
        if (centerPoint != null)
        {
            Gizmos.DrawWireSphere(centerPoint.position, activationRadius);
        }

        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Rage pushback radius
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, pushbackRadius);
    }
    #endregion
}
