using UnityEngine;
 
public class JumpingState:State
{
    bool grounded;
 
    float gravityValue;
    float jumpHeight;
    float playerSpeed;
 
    Vector3 airVelocity;
    Vector3 horizontalVelocity; // Add this to preserve momentum
 
    public JumpingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }
 
    public override void Enter()
    {
        base.Enter();
 
        grounded = false;
        gravityValue = character.gravityValue;
        jumpHeight = character.jumpHeight;
        playerSpeed = character.playerSpeed;
        
        // PRESERVE the player's current horizontal velocity when jumping
        Vector3 currentVelocity = character.controller.velocity;
        horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        
        gravityVelocity.y = 0;
 
        character.animator.SetFloat("speed", 0);
        
        
         // Check if character is falling (not grounded)
        if (character.CheckGrounded())
        {
            character.animator.SetTrigger("jump");
            Jump();
        }
        else
        {
            character.animator.SetTrigger("jump");
        }
       
    }

    
    public override void HandleInput()
    {
        base.HandleInput();
 
        input = moveAction.ReadValue<Vector2>();
    }
 
    public override void LogicUpdate()
    {
        base.LogicUpdate();
 
        if (grounded)
        {
            stateMachine.ChangeState(character.landing);
        }
    }
 
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        // ALWAYS calculate air movement, not just when !grounded
        Vector3 moveDirection = new Vector3(input.x, 0, input.y);
        moveDirection = moveDirection.x * character.cameraTransform.right.normalized + moveDirection.z * character.cameraTransform.forward.normalized;
        moveDirection.y = 0f;
        
        // Rotate character to face movement direction
        if (moveDirection.magnitude > 0.1f) // Only rotate if there's input
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            character.transform.rotation = Quaternion.Slerp(
                character.transform.rotation, 
                targetRotation, 
                Time.deltaTime * character.rotationSpeed // Use your character's rotation speed
            );
        }
        
        // Apply air control to modify the horizontal velocity
        float airSpeed = playerSpeed * character.airControl;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, moveDirection * airSpeed, character.airControl * Time.deltaTime * 10f);
        
        // Apply gravity
        gravityVelocity.y += gravityValue * Time.deltaTime;
        
        // Combine horizontal and vertical movement
        Vector3 totalMovement = (horizontalVelocity + gravityVelocity) * Time.deltaTime;
        character.controller.Move(totalMovement);
        
        grounded = character.controller.isGrounded;
    }
 
    void Jump()
    {
        gravityVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
    }
}