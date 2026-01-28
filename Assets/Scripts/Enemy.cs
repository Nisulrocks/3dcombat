using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
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

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float rotationSpeed = 5f;
    [SerializeField] float stoppingDistance = 0.5f;
    [SerializeField] float rotationThreshold = 15f;
    [SerializeField] float movementBuffer = 0.3f;
    [SerializeField] float gravity = -9.81f;

    [Header("Obstacle Avoidance")]
    [SerializeField] float obstacleDetectionRange = 2f;
    [SerializeField] float avoidanceForce = 2f;
    [SerializeField] int numberOfRays = 5;
    [SerializeField] float raySpreadAngle = 60f;
    [SerializeField] LayerMask obstacleLayer;
    [SerializeField] float sideRayDistance = 1.5f;
    [SerializeField] float wallSlideSpeed = 2f;

    GameObject player;
    Animator animator;
    CharacterController characterController;
    float timePassed;
    bool isMoving = false;
    Vector3 verticalVelocity;
    Vector3 avoidanceDirection = Vector3.zero;

    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

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
    }

    void Update()
    {
        if (player == null)
        {
            animator.SetFloat("speed", 0);
            return;
        }

        float distanceToPlayer = Vector3.Distance(player.transform.position, transform.position);

        // Apply gravity
        if (characterController != null && characterController.isGrounded)
        {
            verticalVelocity.y = 0;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        if (distanceToPlayer <= aggroRange)
        {
            // Calculate direction to player
            Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
            directionToPlayer.y = 0;

            // Detect obstacles and calculate avoidance
            Vector3 avoidance = DetectObstacles();
            
            // Combine player direction with obstacle avoidance
            Vector3 desiredDirection = directionToPlayer + avoidance;
            desiredDirection.y = 0;
            desiredDirection.Normalize();

            if (desiredDirection != Vector3.zero)
            {
                // Rotate towards desired direction
                Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Hysteresis logic for movement
                float stopThreshold = attackRange + stoppingDistance;
                Vector3 horizontalMovement = Vector3.zero;

                if (isMoving)
                {
                    if (distanceToPlayer > stopThreshold + movementBuffer)
                    {
                        horizontalMovement = transform.forward * moveSpeed * Time.deltaTime;
                    }
                    else
                    {
                        isMoving = false;
                    }
                }
                else
                {
                    if (distanceToPlayer > stopThreshold + movementBuffer * 2f)
                    {
                        isMoving = true;
                        horizontalMovement = transform.forward * moveSpeed * Time.deltaTime;
                    }
                }

                // Combine horizontal and vertical movement
                Vector3 totalMovement = horizontalMovement + verticalVelocity * Time.deltaTime;

                if (characterController != null)
                {
                    characterController.Move(totalMovement);
                }
                else
                {
                    transform.position += horizontalMovement;
                }
            }
        }
        else
        {
            isMoving = false;
            if (characterController != null)
            {
                characterController.Move(verticalVelocity * Time.deltaTime);
            }
        }

        // Update animator
        animator.SetFloat("speed", isMoving ? 1f : 0f);

        // Attack logic
        if (timePassed >= attackCD)
        {
            if (distanceToPlayer <= attackRange)
            {
                animator.SetTrigger("attack");
                timePassed = 0;
            }
        }
        timePassed += Time.deltaTime;
    }

    Vector3 DetectObstacles()
    {
        Vector3 avoidanceVector = Vector3.zero;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        // Multi-ray obstacle detection in a cone pattern
        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = -raySpreadAngle / 2 + (raySpreadAngle / (numberOfRays - 1)) * i;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * transform.forward;

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, obstacleDetectionRange, obstacleLayer))
            {
                // Calculate avoidance force based on distance and angle
                float distanceFactor = 1 - (hit.distance / obstacleDetectionRange);
                float angleFactor = 1 - (Mathf.Abs(angle) / (raySpreadAngle / 2));
                
                // Calculate perpendicular avoidance direction
                Vector3 avoidDirection = Vector3.Cross(hit.normal, Vector3.up).normalized;
                
                // Choose direction that moves away from obstacle
                if (Vector3.Dot(avoidDirection, transform.right) < 0)
                {
                    avoidDirection = -avoidDirection;
                }

                avoidanceVector += avoidDirection * distanceFactor * angleFactor * avoidanceForce;

                Debug.DrawRay(rayOrigin, rayDirection * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(rayOrigin, rayDirection * obstacleDetectionRange, Color.green);
            }
        }

        // Side detection for wall sliding
        CheckWallSliding(ref avoidanceVector, rayOrigin);

        return avoidanceVector;
    }

    void CheckWallSliding(ref Vector3 avoidanceVector, Vector3 rayOrigin)
    {
        // Check left side
        RaycastHit leftHit;
        if (Physics.Raycast(rayOrigin, -transform.right, out leftHit, sideRayDistance, obstacleLayer))
        {
            // Push away from left wall
            avoidanceVector += transform.right * avoidanceForce * 0.5f;
            Debug.DrawRay(rayOrigin, -transform.right * leftHit.distance, Color.yellow);
        }

        // Check right side
        RaycastHit rightHit;
        if (Physics.Raycast(rayOrigin, transform.right, out rightHit, sideRayDistance, obstacleLayer))
        {
            // Push away from right wall
            avoidanceVector += -transform.right * avoidanceForce * 0.5f;
            Debug.DrawRay(rayOrigin, transform.right * rightHit.distance, Color.yellow);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            player = collision.gameObject;
        }
    }

    void Die()
    {
        OnDied?.Invoke();
        Instantiate(ragdoll, transform.position, transform.rotation);
        Destroy(this.gameObject);
    }

    public void TakeDamage(float damageAmount)
    {
        health -= damageAmount;
        animator.SetTrigger("damage");

        OnHealthChanged?.Invoke(health, maxHealth);

        if (health <= 0)
        {
            Die();
        }
    }

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
 
    public void HitVFX(Vector3 hitPosition)
    {
        GameObject hit = Instantiate(hitVFX, hitPosition, Quaternion.identity);
        Destroy(hit, 3f);
    }
 
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        
        // Draw obstacle detection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, obstacleDetectionRange);
    }
}