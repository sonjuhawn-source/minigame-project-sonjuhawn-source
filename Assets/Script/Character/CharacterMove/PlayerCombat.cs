using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static CharacterStateMachine;

public enum AttackType
{
    Light1,
    Light2,
    Light3,
    Heavy1,
    Heavy2,
    HeavyDash,        
    HeavyFinisher,    
}

[System.Serializable]
public class AttackEntry
{
    public AttackType type;
    public float multiplier;
}

public class PlayerCombat : MonoBehaviour
{
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int HeavyAttack0Hash = Animator.StringToHash("HeavyAttack0");
    private static readonly int HeavyAttack1Hash = Animator.StringToHash("HeavyAttack1");
    private static readonly int HeavyDashHash = Animator.StringToHash("HeavyDash");
    private static readonly int FinisherHash = Animator.StringToHash("Finisher");
    private static readonly int DodgeHash = Animator.StringToHash("Dodge");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    private static readonly int[] ActionTriggers =
    {
        AttackHash, HeavyAttack0Hash, HeavyAttack1Hash,
        HeavyDashHash, FinisherHash, DodgeHash
    };

    private PlayerInputHandler input;
    private CharacterStateMachine state;
    private StaminaSystem stamina;
    private Animator anim;
    private Rigidbody rb;
    private HealthSystem health;

    private int leftComboIndex = 0;

    [SerializeField] private GameObject hitbox;  
    [SerializeField] private SwordHitbox sword;

    [SerializeField] private float baseAnimSpeed = 1f;
    [SerializeField] private float speedPerAgility = 0.03f;

    [SerializeField] private float dodgeDistance = 3f;
    [SerializeField] private float dodgeDuration = 0.3f;
    [SerializeField] private float dodgeCooldown = 0.4f;
    private float lastDodgeTime = -999f;

    [SerializeField] private int dodgeStaminaCost = 25;
    [SerializeField] private int heavyAttack0Cost = 25;
    [SerializeField] private int heavyAttack1Cost = 30;
    [SerializeField] private int heavyDashCost = 35;
    [SerializeField] private int finisherCost = 50;

    private IStatProvider stats;

    [SerializeField] private List<AttackEntry> attackTable;
    private Dictionary<AttackType, float> multiplierMap;

    [SerializeField] private AudioClip normalCilp;
    [SerializeField] private AudioClip hardAttackCilp;
    [SerializeField] private AudioClip finalAttackCilp;
    [SerializeField] private AudioClip DodgeCilp;

    private bool canChainCombo = false;

    private CancellationTokenSource _liftCts;


    private void Awake()
    {
        input = GetComponent<PlayerInputHandler>();
        state = GetComponent<CharacterStateMachine>();
        health = GetComponent<HealthSystem>();
        stamina = GetComponent<StaminaSystem>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<IStatProvider>();

        multiplierMap = new Dictionary<AttackType, float>();
        foreach (var e in attackTable)
            multiplierMap[e.type] = e.multiplier;
    }

    private void OnEnable()
    {
        input.OnAttack += HandleAttack;
        input.OnHeavyAttack += HandleHeavyAttack;
        input.OnDodge += HandleDodge;
        stats.OnStatChanged += RecalculateAnimSpeed;
        health.OnDeath += HandleDeath;
        RecalculateAnimSpeed();
    }

    private void OnDisable()
    {
        input.OnAttack -= HandleAttack;
        input.OnHeavyAttack -= HandleHeavyAttack;
        input.OnDodge -= HandleDodge;
        health.OnDeath -= HandleDeath;
        stats.OnStatChanged -= RecalculateAnimSpeed;
    }

    // 소비되지 않은 Animator 트리거가 남아 회피 직후 공격이 튀어나오던 문제가 있었다.
    // 하나를 세울 때 나머지를 전부 리셋해 잔존 트리거를 차단한다.
    private void SetTriggerExclusive(int hash)
    {
        for (int i = 0; i < ActionTriggers.Length; i++)
        {
            if (ActionTriggers[i] != hash)
                anim.ResetTrigger(ActionTriggers[i]);
        }
        anim.SetTrigger(hash);
    }

    // 플레이어는 Damaged 상태로 들어가지 않는다 — 다수 피격 시 스턴락이 생겨 의도적으로 뺐다.
    // (PlayerState.Damaged는 enum 5번이지만 Animator의 targetState는 0~4만 쓴다)
    // 아래 Damaged 검사는 되살릴 때를 대비해 둔 게이트이며 현재는 항상 false다.
    private void HandleAttack()
    {
        if (state.CurrentState == PlayerState.Dodging ||
        state.CurrentState == PlayerState.Damaged ||
        state.CurrentState == PlayerState.Dead)
            return;

        SetTriggerExclusive(AttackHash);
    }

    public void OnAttackRecover()
    {
        if (stamina != null)
            stamina.RecoverByStat();
    }

    private void HandleHeavyAttack()
    {
        if (state.CurrentState == PlayerState.Dodging ||
        state.CurrentState == PlayerState.Damaged ||
        state.CurrentState == PlayerState.Dead)
            return;

        if (state.CurrentState == PlayerState.HeavyAttacking)
        {
            if (!canChainCombo) return;
            canChainCombo = false;
        }

        int cost;
        int triggerHash;

        switch (leftComboIndex)
        {
            case 0:
                cost = heavyAttack0Cost;
                triggerHash = HeavyAttack0Hash;
                break;
            case 1:
                cost = heavyAttack1Cost;
                triggerHash = HeavyAttack1Hash;
                break;
            case 2:
                cost = heavyDashCost;
                triggerHash = HeavyDashHash;
                break;
            case 3:
                cost = finisherCost;
                triggerHash = FinisherHash;
                break;
            default: return;
        }

        bool canUse = stamina.TryConsume(cost);

        if (!canUse)
            return;


        state.ChangeState(PlayerState.HeavyAttacking); 
        SetTriggerExclusive(triggerHash);
    }

    public void OpenComboWindow()
    {
        canChainCombo = true;
    }
    public void CloseComboWindow()
    {
        canChainCombo = false;
    }

    public void SetAttackType(AttackType type)
    {
        if (!multiplierMap.TryGetValue(type, out float mult)) mult = 1f;
        int baseDmg = 10;
        int bonus = Mathf.Max(0, stats.Strength - 10) * 1;
        int dmg = Mathf.RoundToInt((baseDmg + bonus) * mult);
        sword.SetDamage(dmg);
    }

    public void OnFinisherJump()
    {
        rb.useGravity = false;
        _liftCts = new CancellationTokenSource();
        LiftUpAsync(_liftCts.Token).Forget();
    }

    public void OnFinisherLand()
    {
        _liftCts?.Cancel();
        DropDownAsync().Forget();
    }

    private async UniTaskVoid LiftUpAsync(CancellationToken ct)
    {
        float elapsed = 0f;
        float liftDuration = 0.2f;
        float liftHeight = 0.5f;
        Vector3 originPos = rb.position;

        while (elapsed < liftDuration)
        {
            if (ct.IsCancellationRequested) return;
            elapsed += Time.deltaTime;
            rb.MovePosition(originPos + Vector3.up * (liftHeight * elapsed / liftDuration));
            await UniTask.WaitForFixedUpdate();
        }
    }

    private async UniTaskVoid DropDownAsync()
    {
        float elapsed = 0f;
        float dropDuration = 0.15f;
        Vector3 currentPos = rb.position;
        Vector3 targetPos = new Vector3(currentPos.x, 0f, currentPos.z);

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            rb.MovePosition(Vector3.Lerp(currentPos, targetPos, elapsed / dropDuration));
            await UniTask.WaitForFixedUpdate();
        }

        rb.MovePosition(targetPos);
        rb.useGravity = true;
    }

    private void HandleDodge()
    {
        if (state.CurrentState == PlayerState.Damaged ||
            state.CurrentState == PlayerState.HeavyAttacking ||
        state.CurrentState == PlayerState.Dead)
            return;

        if (Time.time - lastDodgeTime < dodgeCooldown)
            return;

        if (!stamina.TryConsume(dodgeStaminaCost))
            return;

        lastDodgeTime = Time.time;

        Vector3 moveInput = new Vector3(input.MoveInput.x, 0, input.MoveInput.y);
        Vector3 direction = moveInput.sqrMagnitude > 0.01f
            ? Camera.main.transform.TransformDirection(moveInput).normalized  
            : transform.forward; 
        direction.y = 0;
        direction.Normalize();

        SetTriggerExclusive(DodgeHash);
        anim.Play(DodgeHash, 0, 0f);
        DodgeMoveAsync(direction).Forget();
    }

    private async UniTaskVoid DodgeMoveAsync(Vector3 direction)
    {
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        float savedSpeed = anim.speed;
        anim.speed = 1f;

        float elapsed = 0f;
        Vector3 velocity = direction * (dodgeDistance / dodgeDuration);

        while (elapsed < dodgeDuration)
        {
            rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
            elapsed += Time.deltaTime;
            await UniTask.WaitForFixedUpdate();
        }

        anim.speed = savedSpeed;
    }

    private void HandleDeath()
    {
        state.ChangeState(PlayerState.Dead);
        SetTriggerExclusive(DeadHash);
    }

    public void SetComboIndex(int value)
    {
        leftComboIndex = value;
    }

    public void ResetCombo()
    {
        leftComboIndex = 0;

        for (int i = 0; i < ActionTriggers.Length; i++)
            anim.ResetTrigger(ActionTriggers[i]);
    }
    public void EnableHitbox() => hitbox.SetActive(true);
    public void DisableHitbox() => hitbox.SetActive(false);

    private void RecalculateAnimSpeed()
    {
        int bonus = Mathf.Max(0, stats.Agility - 10);
        anim.speed = baseAnimSpeed + bonus * speedPerAgility; ;
    }

    public void OnNormalAttackSound() => AudioManager.Instance.PlaySFX(normalCilp);
    public void OnHardAttackSound() => AudioManager.Instance.PlaySFX(hardAttackCilp);
    public void OnFinalAttackSound() => AudioManager.Instance.PlaySFX(finalAttackCilp);
    public void OnDodgeSound() => AudioManager.Instance.PlaySFX(DodgeCilp);

}
