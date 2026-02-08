using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    #region Enums
    private enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        ReturnToStart
    }
    #endregion

    #region Inspector Fields
    [SerializeField] float health = 3;
    private float maxHealth;

    public System.Action<float, float> OnHealthChanged;
    public System.Action OnDied;

    public float CurrentHealth => health;
    public float MaxHealth => maxHealth;
    [SerializeField] GameObject hitVFX;
    [SerializeField] GameObject ragdoll;

    [Header("Combat")]
    [SerializeField] float attackCD = 3f;
    [SerializeField] float attackRange = 1f;
    [SerializeField] float aggroRange = 4f;
    [SerializeField] float yLevelThreshold = 5f; // Max Y difference for targeting

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float patrolSpeed = 1.5f;
    [SerializeField] float rotationSpeed = 5f;
    [SerializeField] float gravity = -9.81f;

    [Header("Patrol & Idle")]
    [SerializeField] float patrolRadius = 5f; // How far from spawn point to patrol
    [SerializeField] float minIdleTime = 2f; // Min time to idle before patrolling
    [SerializeField] float maxIdleTime = 5f; // Max time to idle before patrolling
    [SerializeField] float patrolPointReachedDist = 0.5f; // How close to patrol point before picking new one
    [SerializeField] float returnToStartDist = 1f; // How close to spawn before switching to Idle

    [Header("Obstacle Avoidance")]
    [SerializeField] float obstacleDetectionRange = 2f;
    [SerializeField] float avoidanceForce = 2f;
    [SerializeField] int numberOfRays = 5;
    [SerializeField] float raySpreadAngle = 60f;
    [SerializeField] LayerMask obstacleLayer;
    [SerializeField] float sideRayDistance = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip attackSFX;
    [SerializeField] private AudioClip damageSFX;
    [SerializeField] private AudioClip deathSFX;
    [SerializeField] private AudioClip alertSFX;
    #endregion

    #region Private State
    // References
    GameObject player;
    Animator animator;
    CharacterController characterController;
    AudioSource audioSource;

    // FSM
    private EnemyState currentState = EnemyState.Idle;
    private Vector3 spawnPosition;
    private Vector3 currentPatrolTarget;
    private float stateTimer;
    private float attackCooldownTimer;
    private bool hasAlerted = false;
    private Vector3 verticalVelocity;
    private bool isAttacking = false; // Track if attack animation is playing
    private float attackAnimationDuration = 1.5f; // Approximate attack animation duration
    private float attackStartTime;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        maxHealth = health;
        OnHealthChanged?.Invoke(health, maxHealth);

        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.center = new Vector3(0, 1, 0);
        }

        player = GameObject.FindGameObjectWithTag("Player");

        // Cache spawn position for patrol/return behavior
        spawnPosition = transform.position;

        // Start in Idle with a random wait
        ChangeState(EnemyState.Idle);
    }

    public void SetPlayer(GameObject newPlayer)
    {
        player = newPlayer;
    }

    void Update()
    {
        // Apply gravity
        if (characterController != null && characterController.isGrounded)
        {
            verticalVelocity.y = 0;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        // Tick attack cooldown
        if (attackCooldownTimer > 0)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        // Run current state
        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.ReturnToStart:
                UpdateReturnToStart();
                break;
        }
    }
    #endregion

    #region State Machine
    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // Exit old state
        OnStateExit(currentState);

        currentState = newState;

        // Enter new state
        OnStateEnter(newState);
    }

    private void OnStateEnter(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                stateTimer = Random.Range(minIdleTime, maxIdleTime);
                animator.SetFloat("speed", 0f);
                break;

            case EnemyState.Patrol:
                SetRandomPatrolTarget();
                break;

            case EnemyState.Chase:
                // Play alert sound on first detection
                if (!hasAlerted)
                {
                    hasAlerted = true;
                    if (alertSFX != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(alertSFX);
                    }
                }
                break;

            case EnemyState.Attack:
                isAttacking = false;
                attackStartTime = Time.time;
                break;

            case EnemyState.ReturnToStart:
                hasAlerted = false;
                break;
        }
    }

    private void OnStateExit(EnemyState state)
    {
        // Nothing special needed on exit for now
    }
    #endregion

    #region State Updates
    private void UpdateIdle()
    {
        animator.SetFloat("speed", 0f);
        ApplyGravityOnly();

        // Check for player aggro
        if (CanSeePlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Count down idle timer, then patrol
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChangeState(EnemyState.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        // Check for player aggro (higher priority)
        if (CanSeePlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        // Move towards patrol target
        Vector3 direction = (currentPatrolTarget - transform.position);
        direction.y = 0;
        float distToTarget = direction.magnitude;

        if (distToTarget < patrolPointReachedDist)
        {
            // Reached patrol point, go back to idle
            ChangeState(EnemyState.Idle);
            return;
        }

        direction.Normalize();

        // Obstacle avoidance based on movement direction, not transform.forward
        Vector3 avoidance = DetectObstacles(direction);

        // Blend avoidance with desired direction - clamp so it can't overpower
        Vector3 moveDir = direction + Vector3.ClampMagnitude(avoidance, avoidanceForce);
        moveDir.y = 0;

        if (moveDir.magnitude > 0.01f)
        {
            moveDir.Normalize();
        }
        else
        {
            moveDir = direction; // Fallback if avoidance cancels out
        }

        // Rotate towards movement direction
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Move
        Vector3 movement = moveDir * patrolSpeed * Time.deltaTime + verticalVelocity * Time.deltaTime;
        if (characterController != null)
        {
            characterController.Move(movement);
        }

        animator.SetFloat("speed", 0.5f); // Half speed for patrol walk
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            ChangeState(EnemyState.ReturnToStart);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float distanceToSpawn = Vector3.Distance(transform.position, spawnPosition);

        // If player left aggro range or is on different Y level, return to start
        if (distanceToPlayer > aggroRange * 1.5f || !IsPlayerOnSameLevel())
        {
            ChangeState(EnemyState.ReturnToStart);
            return;
        }

        // If close enough to attack and cooldown is ready
        if (distanceToPlayer <= attackRange && attackCooldownTimer <= 0)
        {
            ChangeState(EnemyState.Attack);
            return;
        }

        // Chase the player
        Vector3 directionToPlayer = (player.transform.position - transform.position);
        directionToPlayer.y = 0;
        if (directionToPlayer.magnitude > 0.01f)
        {
            directionToPlayer.Normalize();
        }

        // Obstacle avoidance based on movement direction
        Vector3 avoidance = DetectObstacles(directionToPlayer);

        // Scale avoidance strength for chase speed so obstacles are properly avoided
        float chaseAvoidanceStrength = avoidanceForce * (moveSpeed / patrolSpeed);
        Vector3 moveDir = directionToPlayer + Vector3.ClampMagnitude(avoidance, chaseAvoidanceStrength);
        moveDir.y = 0;

        if (moveDir.magnitude > 0.01f)
        {
            moveDir.Normalize();
        }
        else
        {
            moveDir = directionToPlayer; // Fallback if avoidance cancels out
        }

        // Always face the movement direction (not player) so it navigates around obstacles
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Only move if outside attack range
        Vector3 horizontalMovement = Vector3.zero;
        if (distanceToPlayer > attackRange - 0.1f)
        {
            horizontalMovement = moveDir * moveSpeed * Time.deltaTime;
        }

        Vector3 totalMovement = horizontalMovement + verticalVelocity * Time.deltaTime;
        if (characterController != null)
        {
            characterController.Move(totalMovement);
        }

        animator.SetFloat("speed", horizontalMovement.magnitude > 0.001f ? 1f : 0f);
    }

    private void UpdateAttack()
    {
        if (player == null)
        {
            ChangeState(EnemyState.ReturnToStart);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        float timeSinceAttackStart = Time.time - attackStartTime;

        // Face the player during attack
        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        ApplyGravityOnly();
        animator.SetFloat("speed", 0f);

        // Perform attack if not already attacking and cooldown is ready
        if (!isAttacking && attackCooldownTimer <= 0)
        {
            animator.SetTrigger("attack");
            isAttacking = true;
            attackStartTime = Time.time;

            // Play attack sound
            if (attackSFX != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSFX);
            }

            attackCooldownTimer = attackCD;
        }

        // Only consider state transitions after attack animation has had time to play
        if (timeSinceAttackStart >= attackAnimationDuration)
        {
            // Reset attack flag so next attack can fire after cooldown
            isAttacking = false;

            // If player moved significantly out of attack range, chase again
            if (distanceToPlayer > attackRange + 0.5f)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            // If player left aggro range entirely
            if (distanceToPlayer > aggroRange * 1.5f || !IsPlayerOnSameLevel())
            {
                ChangeState(EnemyState.ReturnToStart);
                return;
            }
        }
    }

    private void UpdateReturnToStart()
    {
        // Check for player aggro while returning
        if (CanSeePlayer())
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        Vector3 direction = (spawnPosition - transform.position);
        direction.y = 0;
        float distToSpawn = direction.magnitude;

        if (distToSpawn < returnToStartDist)
        {
            // Back at spawn, go idle
            ChangeState(EnemyState.Idle);
            return;
        }

        direction.Normalize();

        // Obstacle avoidance based on movement direction
        Vector3 avoidance = DetectObstacles(direction);

        // Blend avoidance with desired direction - clamp so it can't overpower
        Vector3 moveDir = direction + Vector3.ClampMagnitude(avoidance, avoidanceForce);
        moveDir.y = 0;

        if (moveDir.magnitude > 0.01f)
        {
            moveDir.Normalize();
        }
        else
        {
            moveDir = direction; // Fallback if avoidance cancels out
        }

        // Rotate towards movement direction
        if (moveDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Move back at patrol speed
        Vector3 movement = moveDir * patrolSpeed * Time.deltaTime + verticalVelocity * Time.deltaTime;
        if (characterController != null)
        {
            characterController.Move(movement);
        }

        animator.SetFloat("speed", 0.5f);
    }
    #endregion

    #region Helpers
    private bool CanSeePlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        return distanceToPlayer <= aggroRange && IsPlayerOnSameLevel();
    }

    private bool IsPlayerOnSameLevel()
    {
        if (player == null) return false;
        float yDifference = Mathf.Abs(transform.position.y - player.transform.position.y);
        return yDifference <= yLevelThreshold;
    }

    private void SetRandomPatrolTarget()
    {
        // Try to pick a patrol point that isn't blocked by obstacles
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Check if path to candidate is clear
            Vector3 dirToCandidate = (candidate - transform.position);
            dirToCandidate.y = 0;
            float dist = dirToCandidate.magnitude;

            if (dist > 0.5f && !Physics.Raycast(rayOrigin, dirToCandidate.normalized, dist, obstacleLayer))
            {
                currentPatrolTarget = candidate;
                return;
            }
        }

        // Fallback: pick any random point (better than not patrolling)
        Vector2 fallbackCircle = Random.insideUnitCircle * patrolRadius;
        currentPatrolTarget = spawnPosition + new Vector3(fallbackCircle.x, 0, fallbackCircle.y);
    }

    private void ApplyGravityOnly()
    {
        if (characterController != null)
        {
            characterController.Move(verticalVelocity * Time.deltaTime);
        }
    }
    #endregion

    #region Obstacle Avoidance
    Vector3 DetectObstacles(Vector3 desiredDirection)
    {
        Vector3 avoidanceVector = Vector3.zero;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        if (numberOfRays < 2) numberOfRays = 2;

        // Multi-ray obstacle detection in a cone pattern based on MOVEMENT direction
        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = -raySpreadAngle / 2 + (raySpreadAngle / (numberOfRays - 1)) * i;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * desiredDirection;

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, obstacleDetectionRange, obstacleLayer))
            {
                float distanceFactor = 1 - (hit.distance / obstacleDetectionRange);

                // Use hit normal to slide along walls instead of perpendicular cross
                Vector3 slideDirection = Vector3.ProjectOnPlane(desiredDirection, hit.normal).normalized;

                // If slide direction is too small, push perpendicular to the wall
                if (slideDirection.magnitude < 0.1f)
                {
                    slideDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
                    // Pick the side closer to our desired direction
                    if (Vector3.Dot(slideDirection, desiredDirection) < 0)
                    {
                        slideDirection = -slideDirection;
                    }
                }

                avoidanceVector += slideDirection * distanceFactor * avoidanceForce;

                Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.red);
                Debug.DrawRay(hit.point, slideDirection * 2f, Color.yellow);
            }
            else
            {
                Debug.DrawRay(rayOrigin, rayDirection * obstacleDetectionRange, Color.green);
            }
        }

        // Side ray detection to prevent getting stuck on walls approached from the side
        RaycastHit leftHit;
        Vector3 leftDir = Quaternion.Euler(0, -90, 0) * desiredDirection;
        if (Physics.Raycast(rayOrigin, leftDir, out leftHit, sideRayDistance, obstacleLayer))
        {
            float distanceFactor = 1 - (leftHit.distance / sideRayDistance);
            avoidanceVector += -leftDir * distanceFactor * avoidanceForce * 0.5f;
            Debug.DrawRay(rayOrigin, leftDir * leftHit.distance, Color.magenta);
        }

        RaycastHit rightHit;
        Vector3 rightDir = Quaternion.Euler(0, 90, 0) * desiredDirection;
        if (Physics.Raycast(rayOrigin, rightDir, out rightHit, sideRayDistance, obstacleLayer))
        {
            float distanceFactor = 1 - (rightHit.distance / sideRayDistance);
            avoidanceVector += -rightDir * distanceFactor * avoidanceForce * 0.5f;
            Debug.DrawRay(rayOrigin, rightDir * rightHit.distance, Color.magenta);
        }

        return avoidanceVector;
    }
    #endregion

    #region Collision
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            player = collision.gameObject;
        }
    }
    #endregion

    #region Health & Death
    void Die()
    {
        // Play death sound on temporary AudioSource that persists after destruction
        if (deathSFX != null)
        {
            PlayDeathSoundPersistent(deathSFX);
        }

        // Destroy enemy immediately
        DestroyEnemy();
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

    private void DestroyEnemy()
    {
        OnDied?.Invoke();
        Instantiate(ragdoll, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }

    public void TakeDamage(float damageAmount)
    {
        health -= damageAmount;
        animator.SetTrigger("damage");

        // Play damage sound
        if (damageSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSFX);
        }

        OnHealthChanged?.Invoke(health, maxHealth);

        // Getting hit forces chase state if player exists
        if (player != null && health > 0)
        {
            ChangeState(EnemyState.Chase);
        }

        if (health <= 0)
        {
            Die();
        }
    }
    #endregion

    #region Animation Events (called by animator)
    public void StartDealDamage()
    {
        GetComponentInChildren<EnemyDamageDealer>().StartDealDamage();
        
        SwordColliderController colliderController = GetComponentInChildren<SwordColliderController>();
        if (colliderController != null)
        {
            colliderController.StartDealDamage();
        }
    }

    public void EndDealDamage()
    {
        GetComponentInChildren<EnemyDamageDealer>().EndDealDamage();
        
        SwordColliderController colliderController = GetComponentInChildren<SwordColliderController>();
        if (colliderController != null)
        {
            colliderController.EndDealDamage();
        }
    }
    #endregion

    #region VFX
    public void HitVFX(Vector3 hitPosition)
    {
        if (hitVFX == null) return;

        // Instantiate VFX at hit position
        GameObject hit = Instantiate(hitVFX, hitPosition, Quaternion.identity);
        
        // Add FollowTargetVFX component to make it follow this transform
        FollowTargetVFX followComponent = hit.GetComponent<FollowTargetVFX>();
        if (followComponent == null)
        {
            followComponent = hit.AddComponent<FollowTargetVFX>();
        }
        
        // Set target to this transform with offset from hit position
        Vector3 offset = hitPosition - transform.position;
        followComponent.SetTarget(transform, offset);
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        
        // Draw obstacle detection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, obstacleDetectionRange);

        // Draw patrol radius (from spawn point in play mode, from current pos in editor)
        Gizmos.color = Color.blue;
        Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);

        // Draw current patrol target in play mode
        if (Application.isPlaying && currentState == EnemyState.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentPatrolTarget, 0.3f);
            Gizmos.DrawLine(transform.position, currentPatrolTarget);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw Y level threshold
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Vector3 pos = Application.isPlaying ? spawnPosition : transform.position;
        Gizmos.DrawCube(pos, new Vector3(aggroRange * 2, yLevelThreshold * 2, aggroRange * 2));
    }
    #endregion
}