using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [SerializeField] private BossData data;

    private BossBlackboard bb;
    private BTNode root;
    private BossHealth health;


    private void Start()
    {
        bb = new BossBlackboard
        {
            self = transform,
            target = GameObject.FindWithTag("Player").transform,
            anim = GetComponent<Animator>(),
            data = data
        };

        bb.onAttackFired = () => AttackCooldownAsync().Forget();
        bb.onChargeFired = () => ChargeCooldownAsync().Forget();
        bb.onRangedFired = () => RangedCooldownAsync().Forget();

        // BT 트리 조립
        root = new BTSelector(new List<BTNode>
        {
            new ContinueChargeAction(),
            new ContinueTelegraphAction(),
            new BTSequence(new List<BTNode>
            {
                new CheckDistance(data.rangedRange),
                new DecidePatternAction(),
                new BTSelector(new List<BTNode>
                {
                    new MeleeAttackAction(),
                    new ChargeAction(),
                    new RangedAttackAction()
                })
            }),
            new ChaseAction()
        });

        health = GetComponent<BossHealth>();
        health.OnDeath += HandleDeath;
    }
    private void Update()
    {
        if (bb.isAppearing) return;
        bb.anim.SetFloat("Move", 0f);
        root.Execute(bb);
    }
    // 애니메이션 이벤트에서 호출
    public void OnAttackHit()
    {
        if (bb.target == null) return;

        float distance = Vector3.Distance(bb.self.position, bb.target.position);
        if (distance <= data.attackRange + 0.5f)
        {
            var playerHealth = bb.target.GetComponent<HealthSystem>();
            if (playerHealth != null)
                playerHealth.TakeDamage(data.attackPower);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!bb.isCharging) return;  // 돌진 중일 때만
        if (!other.CompareTag("Player")) return;

        var playerHealth = other.GetComponent<HealthSystem>();
        if (playerHealth != null)
            playerHealth.TakeDamage((int)(data.attackPower * 1.5f));
    }

    public void OnRangedAttackFire()
    {
        if (bb.target == null || data.projectilePrefab == null) return;

        Vector3 spawnPos = transform.TransformPoint(data.muzzleLocalOffset); // BossData에 muzzleLocalOffset 추가 필요
        Vector3 dir = (bb.target.position + Vector3.up * 1f - spawnPos).normalized;

        var proj = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);
        var mp = proj.GetComponent<MonsterProjectile>();
        if (mp != null)
            mp.Init(data.attackPower, data.rangedSpeed, dir); // BossData.rangedSpeed 이미 있음
    }

    private void HandleDeath()
    {
        enabled = false;

        GrantRewards();
        bb.anim.SetTrigger("Die");
    }

    // 일반 몬스터와 같은 방식으로 보상을 지급한다 (MonsterController.HandleDeath 참고)
    private void GrantRewards()
    {
        if (bb.target == null)
            return;

        if (bb.target.TryGetComponent<ExperienceSystem>(out var exp))
            exp.AddExp(data.expReward);

        if (bb.target.TryGetComponent<GoldSystem>(out var gold))
            gold.AddGold(data.goldReward);
    }

    // 공격 쿨다운 코루틴
    private async UniTaskVoid AttackCooldownAsync()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(data.attackCooldown));
        bb.isAttackCooldown = false;
    }
    private async UniTaskVoid ChargeCooldownAsync()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(data.chargeCooldown));
        bb.isChargeCooldown = false;
    }
    private async UniTaskVoid RangedCooldownAsync()
    {
        await UniTask.Delay(System.TimeSpan.FromSeconds(data.rangedCooldown));
        bb.isRangedCooldown = false;
    }

    public void OnAppearFinished()
    {
        bb.isAppearing = false;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }
}