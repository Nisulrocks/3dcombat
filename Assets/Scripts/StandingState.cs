using System.Collections;
using UnityEngine;

public class StandingState: State
{  
    float gravityValue;
    bool jump;   
    bool crouch;
    Vector3 currentVelocity;
    bool grounded;
    bool sprint;
    float playerSpeed;
    bool drawWeapon;
    #pragma warning disable CS0414
    float timePassed; 
    #pragma warning restore CS0414

    Vector3 cVelocity;

    public StandingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }
 
    public override void Enter()
    {
        base.Enter();
 
        jump = false;
        crouch = false;
        sprint = false;
        drawWeapon = false;
        input = Vector2.zero;
        timePassed = 0f; 
        
        currentVelocity = Vector3.zero;
        gravityVelocity.y = 0;
 
        velocity = character.playerVelocity;
        playerSpeed = character.playerSpeed;
        grounded = character.controller.isGrounded;
        gravityValue = character.gravityValue;    
        
        
        character.animator.SetFloat("speed", velocity.magnitude);    
    }
 
    public override void HandleInput()
    {
        base.HandleInput();
 
        if (jumpAction.triggered)
        {
            jump = true;
        }
        if (crouchAction.triggered)
        {
            crouch = true;
        }
        if (sprintAction.triggered && character.CanSprint())
        {
            sprint = true;
        }
 
        if (drawWeaponAction.triggered)
        {
            drawWeapon = true;
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

        
        if (ClimbingState.CanStartClimbing(character) && input.y > 0.1f)
        {
            stateMachine.ChangeState(character.climbing);
            return;
        }

        if (sprint)
        {
            stateMachine.ChangeState(character.sprinting);
        }    
        if (jump)
        {
            stateMachine.ChangeState(character.jumping);
        }
        if (crouch)
        {
            stateMachine.ChangeState(character.crouching);
        }
        if (drawWeapon)
        {
            
            stateMachine.ChangeState(character.combatting);
            
            
            
            character.StartCoroutine(DrawWeaponNextFrame());
        }
        
        
        if (!character.CheckGrounded())
        {
            stateMachine.ChangeState(character.jumping);
        }
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
       
        currentVelocity = Vector3.SmoothDamp(currentVelocity, velocity,ref cVelocity, character.velocityDampTime);
        character.controller.Move(currentVelocity * Time.deltaTime * playerSpeed + gravityVelocity * Time.deltaTime);
  
        if (velocity.sqrMagnitude>0)
        {
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, Quaternion.LookRotation(velocity),character.rotationDampTime);
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

    private IEnumerator DrawWeaponNextFrame()
    {
        
        yield return null;
        
        
        if (character.animator != null)
        {
            character.animator.SetTrigger("drawWeapon");
        }
    }

}