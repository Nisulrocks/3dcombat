using UnityEngine;
using UnityEngine.InputSystem;
public class Character : MonoBehaviour
{
    [Header("Controls")]
    public float playerSpeed = 5.0f;
    public float crouchSpeed = 2.0f;
    public float sprintSpeed = 7.0f;
    public float jumpHeight = 0.8f; 
    public float gravityMultiplier = 2;
    public float rotationSpeed = 5f;
    public float crouchColliderHeight = 1.35f;
 
    [Header("Animation Smoothing")]
    [Range(0, 1)]
    public float speedDampTime = 0.1f;
    [Range(0, 1)]
    public float velocityDampTime = 0.9f;
    [Range(0, 1)]
    public float rotationDampTime = 0.2f;
    [Range(0, 5)]
    public float airControl = 0.5f;

    [Header("Stamina System")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina;
    [SerializeField] private float staminaDrainRate = 20f; // Per second while sprinting
    [SerializeField] private float staminaRegenRate = 10f; // Per second while not sprinting
    [SerializeField] private float staminaRegenDelay = 1f; // Delay before regen starts
    private float lastSprintTime;
    private bool canSprint = true;

    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck; 
    [SerializeField] private float groundRadius; 
    [SerializeField] private LayerMask whatIsGround; 
    [SerializeField] private bool isGrounded;
 
    public StateMachine movementSM;
    public StandingState standing;
    public JumpingState jumping;
    public CrouchingState crouching;
    public LandingState landing;
    public SprintState sprinting;
    public SprintJumpState sprintjumping;
    public CombatState combatting;
    public AttackState attacking;
    public SuperAttackState superAttacking;
 
    [HideInInspector]
    public float gravityValue = -9.81f;
    [HideInInspector]
    public float normalColliderHeight;
    [HideInInspector]
    public CharacterController controller;
    [HideInInspector]
    public PlayerInput playerInput;
    [HideInInspector]
    public Transform cameraTransform;
    [HideInInspector]
    public Animator animator;
    [HideInInspector]
    public Vector3 playerVelocity;
 
 
    // Start is called before the first frame update
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        cameraTransform = Camera.main.transform;

        // Lock cursor to game window
        LockCursor();

        movementSM = new StateMachine();
        standing = new StandingState(this, movementSM);
        jumping = new JumpingState(this, movementSM);
        crouching = new CrouchingState(this, movementSM);
        landing = new LandingState(this, movementSM);
        sprinting = new SprintState(this, movementSM);
        sprintjumping = new SprintJumpState(this, movementSM);
        combatting = new CombatState(this, movementSM);
        attacking = new AttackState(this, movementSM);
        superAttacking = new SuperAttackState(this, movementSM);

        movementSM.Initialize(standing);

        normalColliderHeight = controller.height;
        gravityValue *= gravityMultiplier;
        
        // Initialize stamina
        currentStamina = maxStamina;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Handle cursor locking when application loses/gains focus
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            LockCursor();
        }
        else
        {
            UnlockCursor();
        }
    }

    private void Update()
    {
        movementSM.currentState.HandleInput();
        movementSM.currentState.LogicUpdate();
        
        // Update stamina
        UpdateStamina();
    }
 
    private void FixedUpdate()
    {
        movementSM.currentState.PhysicsUpdate();
    }

    public bool CheckGrounded()
    {
        isGrounded=Physics.CheckSphere(groundCheck.position, groundRadius, (int) whatIsGround);
        return isGrounded;
    }

    private void UpdateStamina()
    {
        // Check if currently sprinting
        bool isSprinting = movementSM.currentState == sprinting || movementSM.currentState == sprintjumping;
        
        if (isSprinting && currentStamina > 0)
        {
            // Drain stamina while sprinting
            currentStamina = Mathf.Max(0, currentStamina - staminaDrainRate * Time.deltaTime);
            lastSprintTime = Time.time;
            
            if (currentStamina <= 0)
            {
                canSprint = false;
            }
        }
        else if (!isSprinting && Time.time - lastSprintTime >= staminaRegenDelay)
        {
            // Regenerate stamina after delay
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
            
            if (currentStamina >= maxStamina * 0.2f) // Allow sprinting when at least 20% stamina
            {
                canSprint = true;
            }
        }
    }

    public bool CanSprint()
    {
        return canSprint && currentStamina > 0;
    }

    public float GetStaminaPercentage()
    {
        return currentStamina / maxStamina;
    }

    public void UseStamina(float amount)
    {
        currentStamina = Mathf.Max(0, currentStamina - amount);
        if (currentStamina <= 0)
        {
            canSprint = false;
        }
    }

    public float GetCurrentStamina()
    {
        return currentStamina;
    }

    public float GetMaxStamina()
    {
        return maxStamina;
    }
}
