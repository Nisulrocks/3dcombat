using UnityEngine;
public class AttackState : State
{
    float timePassed;
    float clipLength;
    float clipSpeed;
    bool attack;
    public AttackState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }
 
    public override void Enter()
    {
        base.Enter();
 
        attack = false;
        character.animator.applyRootMotion = true;
        timePassed = 0f;
        character.animator.SetTrigger("attack");
        character.animator.SetFloat("speed", 0f);
        
        // Start combo window with animation duration
        if (ComboManager.Instance != null)
        {
            // Get attack animation clip duration
            AnimatorClipInfo[] clipInfos = character.animator.GetCurrentAnimatorClipInfo(1);
            if (clipInfos.Length > 0)
            {
                float clipLength = clipInfos[0].clip.length;
                float clipSpeed = character.animator.GetCurrentAnimatorStateInfo(1).speed;
                float animationDuration = clipLength / clipSpeed;
                
                ComboManager.Instance.StartComboWindow(animationDuration);
            }
        }
    }
 
    public override void HandleInput()
    {
        base.HandleInput();
 
        if (attackAction.triggered)
        {
            attack = true;
        }
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
 
        timePassed += Time.deltaTime;
        
        // Safely get clip info
        AnimatorClipInfo[] clipInfos = character.animator.GetCurrentAnimatorClipInfo(1);
        if (clipInfos.Length > 0)
        {
            clipLength = clipInfos[0].clip.length;
            clipSpeed = character.animator.GetCurrentAnimatorStateInfo(1).speed;

            if (timePassed >= clipLength / clipSpeed && attack)
            {
                stateMachine.ChangeState(character.attacking);
            }
            if (timePassed >= clipLength / clipSpeed)
            {
                stateMachine.ChangeState(character.combatting);
                character.animator.SetTrigger("move");
            }
        }
        else
        {
            // Fallback if no clip info is available
            if (timePassed >= 1f) // Default attack duration
            {
                stateMachine.ChangeState(character.combatting);
                character.animator.SetTrigger("move");
            }
        }
    }
    public override void Exit()
    {
        base.Exit();
        character.animator.applyRootMotion = false;
        
        // End combo window when attack animation ends
        if (ComboManager.Instance != null)
        {
            ComboManager.Instance.EndComboWindow();
        }
    }
}