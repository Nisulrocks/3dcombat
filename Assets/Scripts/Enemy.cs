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
    [SerializeField] float rotationThreshold = 15f; // Angle threshold before moving
    [SerializeField] float movementBuffer = 0.3f; // Buffer to prevent rapid start/stop
    [SerializeField] float gravity = -9.81f;

    GameObject player;
    Animator animator;
    CharacterController characterController;
    Rigidbody rigidbody;
    float timePassed;
    float newDestinationCD = 0.5f;
    Vector3 moveDirection;
    bool isMoving = false; // Track movement state for hysteresis
    Vector3 verticalVelocity;

    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        maxHealth = health;
        OnHealthChanged?.Invoke(health, maxHealth);

        // Add CharacterController if it doesn't exist
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

        // Simple rotation and movement
        if (distanceToPlayer <= aggroRange)
        {
            // Calculate direction to player
            Vector3 direction = (player.transform.position - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                // Rotate towards player
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.05f);

                // Hysteresis logic for movement
                float stopThreshold = attackRange + stoppingDistance;
                Vector3 horizontalMovement = Vector3.zero;

                if (isMoving)
                {
                    // Continue moving until well beyond stop distance
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
                    // Start moving only when clearly beyond start distance
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
            // Apply gravity even when not moving
            if (characterController != null)
            {
                characterController.Move(verticalVelocity * Time.deltaTime);
            }
        }

        // Update animator based on movement state
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
        newDestinationCD -= Time.deltaTime;
    }

    void MoveTowardsPlayer()
    {
        // Move forward in the direction the enemy is facing
        moveDirection = transform.forward;
    }

    void RotateTowardsPlayer()
    {
        Vector3 direction = (player.transform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // Use a smaller rotation speed multiplier for smoother rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
        }
    }

    bool IsFacingPlayer()
    {
        Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
        directionToPlayer.y = 0;

        Vector3 forward = transform.forward;
        forward.y = 0;

        float angle = Vector3.Angle(forward, directionToPlayer);
        return angle <= rotationThreshold;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            print(true);
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
        //CameraShake.Instance.ShakeCamera(2f, 0.2f);

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
    }
}