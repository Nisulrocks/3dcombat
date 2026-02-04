using UnityEngine;

public class ClimbingState : State
{
    // Climbing settings
    private float climbSpeed;
    private LayerMask ladderLayer;
    private LayerMask groundLayer;
    private float ladderDetectionDistance = 1f;
    
    // State tracking
    private float verticalInput;
    private Transform currentLadder;
    private Vector3 ladderSnapPosition;
    private bool hasEnteredState = false; // Prevent animation spam
    private float dismountOffset = 1.5f; // How far to move player when dismounting at top
    private float jumpOffForce = 2f; // How far to push player back when jumping off
    private bool jumpPressed = false;
    
    public ClimbingState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }
    
    // Reset state for respawn
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
        
        // Get climbing settings from character
        climbSpeed = character.climbSpeed;
        ladderLayer = character.ladderLayer;
        groundLayer = character.groundLayer;
        ladderDetectionDistance = character.ladderDetectionDistance;
        
        // Find and snap to ladder
        if (DetectLadder(out RaycastHit hit))
        {
            currentLadder = hit.transform;
            
            // Snap player to ladder position (in front of ladder)
            Vector3 snapPos = hit.point + hit.normal * 0.3f;
            snapPos.y = character.transform.position.y; // Keep current height
            character.transform.position = snapPos;
            
            // Face the ladder
            character.transform.rotation = Quaternion.LookRotation(-hit.normal);
        }
        
        // DISABLE ROOT MOTION so we control movement via code
        character.animator.applyRootMotion = false;
        
        // Reset any lingering animator states
        character.animator.SetBool("isClimbing", false);
        character.animator.SetFloat("climbSpeed", 0f);
        
        // Set climbing animation state ONLY ONCE
        if (!hasEnteredState)
        {
            character.animator.SetBool("isClimbing", true);
            character.animator.SetFloat("climbSpeed", 0f);
            hasEnteredState = true;
        }
        
        // Reset state
        verticalInput = 0f;
        jumpPressed = false;
        
        Debug.Log("Entered Climbing State - Root Motion DISABLED");
    }

    public override void HandleInput()
    {
        base.HandleInput();
        
        // Read vertical input for climbing up/down
        input = moveAction.ReadValue<Vector2>();
        verticalInput = input.y; // W = positive (up), S = negative (down)
        
        // Check for jump to dismount
        if (jumpAction.triggered)
        {
            jumpPressed = true;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        // Update climb animation speed based on input
        // Using the animator's speed multiplier on the clip
        character.animator.SetFloat("climbSpeed", Mathf.Abs(verticalInput));
        
        // Check for jump to dismount (push back off ladder)
        if (jumpPressed)
        {
            JumpOffLadder();
            return;
        }
        
        // Check if still on ladder
        if (!IsOnLadder())
        {
            // Exit climbing - use move trigger
            ExitClimbing();
            return;
        }
        
        // Check if reached top of ladder and there's a platform
        if (HasReachedTopWithPlatform())
        {
            DismountAtTop();
            ExitClimbing();
            return;
        }
        
        // Check if reached bottom of ladder
        if (HasReachedBottom())
        {
            ExitClimbing();
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        // Move up/down on ladder (no gravity)
        Vector3 climbMovement = Vector3.up * verticalInput * climbSpeed * Time.deltaTime;
        character.controller.Move(climbMovement);
    }

    public override void Exit()
    {
        base.Exit();
        
        // RE-ENABLE ROOT MOTION
        character.animator.applyRootMotion = true;
        
        // Reset climbing animation
        character.animator.SetBool("isClimbing", false);
        character.animator.SetFloat("climbSpeed", 0f);
        
        currentLadder = null;
        hasEnteredState = false; // Reset for next climb
        jumpPressed = false;
        
        Debug.Log("Exited Climbing State - Root Motion RE-ENABLED");
    }
    
    private void ExitClimbing()
    {
        character.animator.SetTrigger("move");
        stateMachine.ChangeState(character.standing);
    }
    
    // Jump off ladder - push player back
    private void JumpOffLadder()
    {
        // Push player backwards (away from ladder)
        Vector3 jumpOffPosition = character.transform.position;
        jumpOffPosition -= character.transform.forward * jumpOffForce; // Move backward
        character.transform.position = jumpOffPosition;
        
        Debug.Log("Jumped off ladder!");
        ExitClimbing();
    }

    // Detect ladder in front of player
    public bool DetectLadder(out RaycastHit hit)
    {
        // Raycast from player center forward
        Vector3 rayOrigin = character.transform.position + Vector3.up * 1f; // Chest height
        Vector3 rayDirection = character.transform.forward;
        
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, ladderDetectionDistance, ladderLayer))
        {
            Debug.Log($"Ladder detected: {hit.transform.name}");
            return true;
        }
        
        return false;
    }

    // Check if player is still on the ladder
    private bool IsOnLadder()
    {
        if (currentLadder == null) return false;
        
        // Raycast forward to check if still facing ladder
        Vector3 rayOrigin = character.transform.position + Vector3.up * 1f;
        Vector3 rayDirection = character.transform.forward;
        
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, ladderDetectionDistance * 1.5f, ladderLayer))
        {
            return hit.transform == currentLadder;
        }
        
        return false;
    }

    // Check if reached top of ladder AND there's a platform to stand on
    private bool HasReachedTopWithPlatform()
    {
        if (currentLadder == null) return false;
        if (verticalInput <= 0) return false; // Only check when climbing up
        
        // Get ladder bounds
        Collider ladderCollider = currentLadder.GetComponent<Collider>();
        if (ladderCollider == null) return false;
        
        Bounds bounds = ladderCollider.bounds;
        float playerY = character.transform.position.y;
        
        // Check if near top of ladder
        if (playerY < bounds.max.y - 1f) return false;
        
        // Raycast forward and down to check for platform (ground layer)
        Vector3 rayOrigin = character.transform.position + Vector3.up * 1.5f + character.transform.forward * 0.5f;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 3f, groundLayer))
        {
            // Found ground in front - there's a platform to dismount to
            Debug.Log($"Platform detected at top: {hit.point}");
            return true;
        }
        
        return false;
    }
    
    // Check if reached bottom of ladder
    private bool HasReachedBottom()
    {
        if (currentLadder == null) return false;
        if (verticalInput >= 0) return false; // Only check when climbing down
        
        // Check if grounded
        if (character.controller.isGrounded)
        {
            Debug.Log("Reached bottom of ladder - grounded");
            return true;
        }
        
        return false;
    }
    
    // Move player forward and up when reaching top of ladder
    private void DismountAtTop()
    {
        // Raycast to find exact platform position
        Vector3 rayOrigin = character.transform.position + Vector3.up * 1.5f + character.transform.forward * dismountOffset;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 3f, groundLayer))
        {
            // Move player to platform
            Vector3 dismountPosition = hit.point + Vector3.up * 0.1f; // Slightly above ground
            character.transform.position = dismountPosition;
            Debug.Log($"Dismounted at top to platform: {dismountPosition}");
        }
        else
        {
            // Fallback - just move forward
            Vector3 dismountPosition = character.transform.position;
            dismountPosition += character.transform.forward * dismountOffset;
            dismountPosition.y += 0.5f;
            character.transform.position = dismountPosition;
            Debug.Log($"Dismounted at top (fallback): {dismountPosition}");
        }
    }

    // Static helper to check if player can start climbing (called from other states)
    public static bool CanStartClimbing(Character character)
    {
        Vector3 rayOrigin = character.transform.position + Vector3.up * 1f;
        Vector3 rayDirection = character.transform.forward;
        
        Debug.Log($"Checking for ladder - Origin: {rayOrigin}, Direction: {rayDirection}, Distance: {character.ladderDetectionDistance}, Layer: {character.ladderLayer}");
        
        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, character.ladderDetectionDistance, character.ladderLayer))
        {
            Debug.Log($"Ladder found: {hit.transform.name} at {hit.point}");
            return true;
        }
        else
        {
            Debug.Log("No ladder detected");
            return false;
        }
    }
}
