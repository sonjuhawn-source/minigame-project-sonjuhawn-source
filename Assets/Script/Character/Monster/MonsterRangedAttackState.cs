using UnityEngine;

public class MonsterRangedAttackState : IMonsterState
{
    private float endTime;

    public void Enter(MonsterController ctx)
    {
        ctx.FacePlayer();
        ctx.Anim.SetTrigger(MonsterController.RangedHash);
        endTime = Time.time + ctx.Data.rangedRecoveryTime;
    }

    public void Tick(MonsterController ctx)
    {
        if (ctx.Target == null) return;
        ctx.FacePlayer();

        // 회복 중에는 제자리에서 기다린다. 후퇴는 Chase로 돌아간 뒤에 처리한다.
        if (Time.time >= endTime)
            ctx.ChangeState(new MonsterChaseState());
    }

    public void Exit(MonsterController ctx)
    {
        ctx.Anim.ResetTrigger(MonsterController.RangedHash);
    }
}