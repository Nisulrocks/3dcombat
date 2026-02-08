using UnityEngine;
 
public class JumpingState:State
{
    bool grounded;
 
    float gravityValue;
    float jumpHeight;
    float playerSpeed;
 
    Vector3 airVelocity;
    Vector3 horizontalVelocity; 
 
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
        
        
        Vector3 currentVelocity = character.controller.velocity;
        horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        
        gravityVelocity.y = 0;
 
        character.animator.SetFloat("speed", 0);
        
        
         
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
        
        
        Vector3 moveDirection = new Vector3(input.x, 0, input.y);
        moveDirection = moveDirection.x * character.cameraTransform.right.normalized + moveDirection.z * character.cameraTransform.forward.normalized;
        moveDirection.y = 0f;
        
        
        if (moveDirection.magnitude > 0.1f) 
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            character.transform.rotation = Quaternion.Slerp(
                character.transform.rotation, 
                targetRotation, 
                Time.deltaTime * character.rotationSpeed 
            );
        }
        
        
        float airSpeed = playerSpeed * character.airControl;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, moveDirection * airSpeed, character.airControl * Time.deltaTime * 10f);
        
        
        gravityVelocity.y += gravityValue * Time.deltaTime;
        
        
        Vector3 totalMovement = (horizontalVelocity + gravityVelocity) * Time.deltaTime;
        character.controller.Move(totalMovement);
        
        grounded = character.controller.isGrounded;
    }
 
    void Jump()
    {
        gravityVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
    }
}