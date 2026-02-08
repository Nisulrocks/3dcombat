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
        
        
        attackAction.Reset();
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

        
        float targetSpeed = input.magnitude;
        
        
        if (targetSpeed < 0.1f)
        {
            targetSpeed = 0f;
        }
        
        character.animator.SetFloat("speed", targetSpeed, character.speedDampTime, Time.deltaTime);

        if (sheathWeapon)
        {
            character.animator.SetTrigger("sheathWeapon");
            
            
            
            if (timePassed > 0.1f) 
            {
                stateMachine.ChangeState(character.standing);
            }
        }

        if (attack)
        {
            
            ShieldSystem shieldSystem = character.GetComponent<ShieldSystem>();
            if (shieldSystem != null && shieldSystem.CurrentShield != null)
            {
                
                attack = false;
                return;
            }

            
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
            
            ShieldSystem shieldSystem = character.GetComponent<ShieldSystem>();
            if (shieldSystem != null && shieldSystem.CanBlock)
            {
                character.animator.SetTrigger("block");
                
                
            }
            block = false; 
        }

        
        if (superActivate)
        {
            if (SuperSystem.Instance != null && SuperSystem.Instance.IsSuperReady)
            {
                
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
        
        
        if (input.magnitude < 0.01f)
        {
            character.animator.SetFloat("speed", 0f);
        }
    }
 
    public override void Exit()
    {
        base.Exit();

        gravityVelocity.y = 0f;
        character.playerVelocity = new Vector3(input.x, 0, input.y);
        
        
        character.animator.SetFloat("speed", 0f);

        if (velocity.sqrMagnitude > 0)
        {
            character.transform.rotation = Quaternion.LookRotation(velocity);
        }

    }
 
}