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

        float distance = Vector3.Distance(ctx.transform.position, ctx.Target.position);
        if (ctx.Data.kiteDistance > 0f && distance < ctx.Data.kiteDistance)
            ctx.Retreat();

        if (Time.time >= endTime)
            ctx.ChangeState(new MonsterChaseState());
    }

    public void Exit(MonsterController ctx)
    {
        ctx.Anim.ResetTrigger(MonsterController.RangedHash);
    }
}