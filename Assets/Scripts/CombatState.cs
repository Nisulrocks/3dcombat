using UnityEngine;
public class CombatState : State
{
    float gravityValue;
    Vector3 currentVelocity;
    bool grounded;
    bool sheathWeapon;
    float playerSpeed;
    bool attack;
    bool block;
    bool superActivate;
    float timePassed;

    Vector3 cVelocity;

    public CombatState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }
 
    public override void Enter()
    {
        base.Enter();
        character.animator.applyRootMotion = true;
        sheathWeapon = false;
        input = Vector2.zero;
        currentVelocity = Vector3.zero;
        gravityVelocity.y = 0;
        attack = false;
        block = false;
        superActivate = false;
        timePassed = 0f;

        velocity = character.playerVelocity;
        playerSpeed = character.playerSpeed;
        grounded = character.controller.isGrounded;
        gravityValue = character.gravityValue;
    }
 
    public override void HandleInput()
    {
        base.HandleInput();

        if (drawWeaponAction.triggered)
        {
            sheathWeapon = true;
        }

        if (attackAction.triggered)
        {
            attack = true;
        }

        if (blockAction.triggered)
        {
            block = true;
        }

        if (superAction.triggered)
        {
            superActivate = true;
        }

        input = moveAction.ReadValue<Vector2>();
        velocity = new Vector3(input.x, 0, input.y);

        velocity = velocity.x * character.cameraTransform.right.normalized + velocity.z * character.cameraTransform.forward.normalized;
        velocity.y = 0f;

    }
 
    public override void LogicUpdate()
    {
        base.LogicUpdate();

        character.animator.SetFloat("speed", input.magnitude, character.speedDampTime, Time.deltaTime);

        if (sheathWeapon)
        {
            character.animator.SetTrigger("sheathWeapon");
            // Only change state after sheathing animation starts
            // The actual state change should be handled by animation events or a timer
            // For now, add a small delay to prevent immediate state change
            if (timePassed > 0.1f) // Small delay to ensure animation starts
            {
                stateMachine.ChangeState(character.standing);
            }
        }

        if (attack)
        {
            // Check if shield is active - if so, prevent attack
            ShieldSystem shieldSystem = character.GetComponent<ShieldSystem>();
            if (shieldSystem != null && shieldSystem.CurrentShield != null)
            {
                // Shield is active, don't allow attack
                attack = false;
                return;
            }

            // Check if super is active - trigger super attack instead
            if (SuperSystem.Instance != null && SuperSystem.Instance.IsSuperActive)
            {
                stateMachine.ChangeState(character.superAttacking);
                attack = false;
                return;
            }
            
            character.animator.SetTrigger("attack");
            stateMachine.ChangeState(character.attacking);
        }

        if (block)
        {
            // Check if shield system can block
            ShieldSystem shieldSystem = character.GetComponent<ShieldSystem>();
            if (shieldSystem != null && shieldSystem.CanBlock)
            {
                character.animator.SetTrigger("block");
                // Block is handled in the same state, no state change needed
                // The animation will handle showing/hiding the shield
            }
            block = false; // Reset block flag
        }

        // Handle super activation
        if (superActivate)
        {
            if (SuperSystem.Instance != null && SuperSystem.Instance.IsSuperReady)
            {
                // Activate super mode
                SuperSystem.Instance.TryActivateSuper();
            }
            superActivate = false;
        }
        
        timePassed += Time.deltaTime;
    }
 
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        gravityVelocity.y += gravityValue * Time.deltaTime;
        grounded = character.controller.isGrounded;
 
        if (grounded && gravityVelocity.y < 0)
        {
            gravityVelocity.y = 0f;
        }
 
        currentVelocity = Vector3.SmoothDamp(currentVelocity, velocity, ref cVelocity, character.velocityDampTime);
        character.controller.Move(currentVelocity * Time.deltaTime * playerSpeed + gravityVelocity * Time.deltaTime);
 
        if (velocity.sqrMagnitude > 0)
        {
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, Quaternion.LookRotation(velocity), character.rotationDampTime);
        }
 
    }
 
    public override void Exit()
    {
        base.Exit();
 
        gravityVelocity.y = 0f;
        character.playerVelocity = new Vector3(input.x, 0, input.y);
 
        if (velocity.sqrMagnitude > 0)
        {
            character.transform.rotation = Quaternion.LookRotation(velocity);
        }
 
    }
 
}