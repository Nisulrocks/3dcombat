using UnityEngine;

public class SuperAttackState : State
{
    private float timePassed;
    private float clipLength;
    private float clipSpeed;
    private bool hasTriggeredSuper;

    public SuperAttackState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();
        timePassed = 0f;
        hasTriggeredSuper = false;
        
        character.animator.applyRootMotion = true;
        
        // Disable player movement by not processing any input
        // Movement is handled in PhysicsUpdate which we override to do nothing
        // Note: Invincibility is already handled by SuperSystem when super is activated
        
        // Trigger super animation
        character.animator.SetTrigger("super");
        
        Debug.Log("Entered SuperAttackState - Movement disabled");
    }

    public override void HandleInput()
    {
        // Don't process any input during super attack
        // This effectively disables all player control
    }

    public override void LogicUpdate()
    {
        // Don't call base - we don't want any normal logic
        timePassed += Time.deltaTime;

        // Get current animation clip info from combat layer (layer 1)
        AnimatorClipInfo[] clipInfos = character.animator.GetCurrentAnimatorClipInfo(1);
        if (clipInfos.Length > 0)
        {
            clipLength = clipInfos[0].clip.length;
            clipSpeed = character.animator.GetCurrentAnimatorStateInfo(1).speed;

            // Check if animation is complete
            if (timePassed >= clipLength / clipSpeed)
            {
                // End super and return to combat state
                if (SuperSystem.Instance != null)
                {
                    SuperSystem.Instance.EndSuper();
                }
                
                stateMachine.ChangeState(character.combatting);
            }
        }
        else
        {
            // Fallback if no clip info
            if (timePassed >= 3f) // Default super duration
            {
                if (SuperSystem.Instance != null)
                {
                    SuperSystem.Instance.EndSuper();
                }
                
                stateMachine.ChangeState(character.combatting);
            }
        }
    }

    public override void PhysicsUpdate()
    {
        // Don't call base and don't move the player
        // Player movement is completely disabled during super attack
    }

    public override void Exit()
    {
        base.Exit();
        
        // Note: Invincibility is removed by SuperSystem.EndSuper()
        
        // Ensure time is restored
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        Debug.Log("Exited SuperAttackState");
    }
}
