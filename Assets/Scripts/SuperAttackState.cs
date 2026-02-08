using UnityEngine;

public class SuperAttackState : State
{
    private float timePassed;
    private float clipLength;
    private float clipSpeed;
    #pragma warning disable CS0414
    private bool hasTriggeredSuper;
    #pragma warning restore CS0414

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
        
        
        
        
        
        
        character.animator.SetTrigger("super");
        
        Debug.Log("Entered SuperAttackState - Movement disabled");
    }

    public override void HandleInput()
    {
        
        
    }

    public override void LogicUpdate()
    {
        
        timePassed += Time.deltaTime;

        
        AnimatorClipInfo[] clipInfos = character.animator.GetCurrentAnimatorClipInfo(1);
        if (clipInfos.Length > 0)
        {
            clipLength = clipInfos[0].clip.length;
            clipSpeed = character.animator.GetCurrentAnimatorStateInfo(1).speed;

            
            if (timePassed >= clipLength / clipSpeed)
            {
                
                if (SuperSystem.Instance != null)
                {
                    SuperSystem.Instance.EndSuper();
                }
                
                stateMachine.ChangeState(character.combatting);
            }
        }
        else
        {
            
            if (timePassed >= 3f) 
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
        
        
    }

    public override void Exit()
    {
        base.Exit();
        
        
        
        
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        Debug.Log("Exited SuperAttackState");
    }
}
