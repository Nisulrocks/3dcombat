using UnityEngine;
public class SprintJumpState:State
{
    float timePassed;
    float jumpTime;
    Vector3 velocity;
    Vector3 input;
 
    public SprintJumpState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }
 
    public override void Enter()
    {
        base.Enter();
        character.animator.applyRootMotion = true;
        timePassed = 0f;
        character.animator.SetTrigger("sprintJump");
 
        jumpTime = 1f;
        input = Vector2.zero;
        velocity = Vector3.zero;
    }
 
    public override void Exit()
    {
        base.Exit();
    }
 
    public override void HandleInput()
    {
        base.HandleInput();
        input = moveAction.ReadValue<Vector2>();
        velocity = new Vector3(input.x, 0, input.y);
        velocity = velocity.x * character.cameraTransform.right.normalized + velocity.z * character.cameraTransform.forward.normalized;
        velocity.y = 0f;
    }
 
    public override void LogicUpdate()
    {
        
        base.LogicUpdate();
        if (timePassed> jumpTime)
        {
            stateMachine.ChangeState(character.sprinting);
        }
        timePassed += Time.deltaTime;
    }
 
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        // Apply gravity
        gravityVelocity.y += character.gravityValue * Time.deltaTime;
        
        // Apply air control movement (reduced for better control)
        Vector3 airMovement = velocity * (character.airControl * 0.5f) * character.sprintSpeed * Time.deltaTime;
        Vector3 totalMovement = airMovement + gravityVelocity * Time.deltaTime;
        
        character.controller.Move(totalMovement);
        
        // Rotate towards movement direction
        if (velocity.magnitude > 0.1f)
        {
            character.transform.rotation = Quaternion.Slerp(character.transform.rotation, Quaternion.LookRotation(velocity), character.rotationDampTime);
        }
    }
 
 
 
}