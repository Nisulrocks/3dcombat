using UnityEngine;

public class ClimbingState : State
{
    
    private float climbSpeed;
    private LayerMask ladderLayer;
    private LayerMask groundLayer;
    private float ladderDetectionDistance = 1f;
    
    
    private float verticalInput;
    private Transform currentLadder;
    private Vector3 ladderSnapPosition;
    private bool hasEnteredState = false; 
    #pragma warning disable CS0414
    private float dismountOffset = 1.5f; 
    #pragma warning restore CS0414
    private float jumpOffForce = 2f; 
    private bool jumpPressed = false;
    
    public ClimbingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }
    
    
    public void ResetState()
    {
        hasEnteredState = false;
        currentLadder = null;
        verticalInput = 0f;
        jumpPressed = false;
        Debug.Log("ClimbingState: Reset for respawn");
    }

    public override void Enter()
    {
        base.Enter();
        
        
        climbSpeed = character.climbSpeed;
        ladderLayer = character.ladderLayer;
        groundLayer = character.groundLayer;
        ladderDetectionDistance = character.ladderDetectionDistance;
        
        
        if (DetectLadder(out RaycastHit hit))
        {
            currentLadder = hit.transform;
            
            
            Vector3 snapPos = hit.point + hit.normal * 0.3f;
            snapPos.y = character.transform.position.y; 
            character.transform.position = snapPos;
            
            
            character.transform.rotation = Quaternion.LookRotation(-hit.normal);
        }
        
        
        character.animator.applyRootMotion = false;
        
        
        character.animator.SetBool("isClimbing", false);
        character.animator.SetFloat("climbSpeed", 0f);
        
        
        if (!hasEnteredState)
        {
            character.animator.SetBool("isClimbing", true);
            character.animator.SetFloat("climbSpeed", 0f);
            hasEnteredState = true;
        }
        
        
        verticalInput = 0f;
        jumpPressed = false;
        
        Debug.Log("Entered Climbing State - Root Motion DISABLED");
    }

    public override void HandleInput()
    {
        base.HandleInput();
        
        
        input = moveAction.ReadValue<Vector2>();
        verticalInput = input.y; 
        
        
        if (jumpAction.triggered)
        {
            jumpPressed = true;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        
        
        character.animator.SetFloat("climbSpeed", Mathf.Abs(verticalInput));
        
        
        if (jumpPressed)
        {
            JumpOffLadder();
            return;
        }
        
        
        if (!IsOnLadder())
        {
            
            ExitClimbing();
            return;
        }
        
        
        if (HasReachedBottom())
        {
            ExitClimbing();
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        
        Vector3 climbMovement = Vector3.up * verticalInput * climbSpeed * Time.deltaTime;
        character.controller.Move(climbMovement);
    }

    public override void Exit()
    {
        base.Exit();
        
        
        character.animator.applyRootMotion = true;
        
        
        character.animator.SetBool("isClimbing", false);
        character.animator.SetFloat("climbSpeed", 0f);
        
        currentLadder = null;
        hasEnteredState = false; 
        jumpPressed = false;
        
        Debug.Log("Exited Climbing State - Root Motion RE-ENABLED");
    }
    
    private void ExitClimbing()
    {
        character.animator.SetTrigger("move");
        stateMachine.ChangeState(character.standing);
    }
    
    
    private void JumpOffLadder()
    {
        
        Vector3 jumpOffPosition = character.transform.position;
        jumpOffPosition -= character.transform.forward * jumpOffForce; 
        character.transform.position = jumpOffPosition;
        
        Debug.Log("Jumped off ladder!");
        ExitClimbing();
    }

    
    public bool DetectLadder(out RaycastHit hit)
    {
        
        Vector3 rayOrigin = character.transform.position + Vector3.up * 1f; 
        Vector3 rayDirection = character.transform.forward;
        
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, ladderDetectionDistance, ladderLayer))
        {
            Debug.Log($"Ladder detected: {hit.transform.name}");
            return true;
        }
        
        return false;
    }

    
    private bool IsOnLadder()
    {
        if (currentLadder == null) return false;
        
        
        Vector3 rayOrigin = character.transform.position + Vector3.up * 1f;
        Vector3 rayDirection = character.transform.forward;
        
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, ladderDetectionDistance * 1.5f, ladderLayer))
        {
            return hit.transform == currentLadder;
        }
        
        return false;
    }

    
    private bool HasReachedBottom()
    {
        if (currentLadder == null) return false;
        if (verticalInput >= 0) return false; 
        
        
        return character.CheckGrounded();
    }

    
    public static bool CanStartClimbing(Character character)
    {
        Vector3 rayOrigin = character.transform.position + Vector3.up * 1f;
        Vector3 rayDirection = character.transform.forward;
        
        
        
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, character.ladderDetectionDistance, character.ladderLayer))
        {
            
            return true;
        }
        else
        {
            
            return false;
        }
    }
}
