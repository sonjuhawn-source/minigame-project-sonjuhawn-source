using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;


public enum WaveState
{
    Idle,        // 시작 전
    Preparing,   // 웨이브 시작 직전 대기 중 (waveDelay)
    Spawning,    // 몬스터들을 풀어놓는 중 (spawnDuration 간격으로)
    InProgress,  // 다 풀어놨고 살아있는 몬스터 처치 대기 중
    Cleared,     // 한 웨이브 클리어 (다음 웨이브로 넘어가기 전 짧은 텀)
    Rest,        // 휴식 타임
    AllCleared   // 모든 웨이브 끝 → 보스 트리거
}

public class WaveManager : MonoBehaviour
{
    [SerializeField] WaveData[] waves;
    [SerializeField] SpawnPoint[] spawnPoints;
    [SerializeField] private GameObject Panelroot;
    [SerializeField] private HealthSystem playerHealth;
    [SerializeField] private ShopUI shopUI;

    private float startTime;
    public float ElapsedTime { get; private set; }
    public int CurrentWaveIndex { get; private set; } = -1;
    public int AliveCount { get; private set; }
    public WaveState State { get; private set; } = WaveState.Idle;
    public int TotalWaves => waves?.Length ?? 0;
    public int TotalKills { get; private set; }


    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCleared;
    public event Action<int> OnAliveCountChanged;
    public event Action OnAllWavesCleared;

    private CancellationTokenSource _waveCts;


    private void OnEnable()
    {
        playerHealth.OnDeath += HandlePlayerDeath;
        DeadEndBehaviour.OnDeadAnimFinished += ShowRestartUI;
    }

    private void OnDisable()
    {
        playerHealth.OnDeath -= HandlePlayerDeath;
        DeadEndBehaviour.OnDeadAnimFinished -= ShowRestartUI;
    }

    private void Start()
    {
        startTime = Time.time;

        if (spawnPoints == null || spawnPoints.Length == 0)
            spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        _waveCts = new CancellationTokenSource();
        RunWavesAsync(_waveCts.Token).Forget();
    }

    private void SpawnOne(MonsterData data, Transform at)
    {
        GameObject prefab = data.prefabOverride;
        var monster = Instantiate(prefab, at.position, at.rotation);
        monster.GetComponent<MonsterController>().SetData(data);
        var health = monster.GetComponent<MonsterHealth>();
        AliveCount++;
        OnAliveCountChanged?.Invoke(AliveCount);

        Action onDeath = null;
        onDeath = () =>
        {
            health.OnDeath -= onDeath;
            AliveCount--;
            OnAliveCountChanged?.Invoke(AliveCount);
            TotalKills++;
        };
        health.OnDeath += onDeath;
    }

    private void HandlePlayerDeath()
    {
        _waveCts?.Cancel();
        Cursor.lockState = CursorLockMode.None;

        var inputProvider = FindAnyObjectByType<CinemachineCamera>();
        if (inputProvider != null) inputProvider.enabled = false;

        foreach (var m in FindObjectsByType<MonsterController>(FindObjectsSortMode.None))
        {
            m.ChangeState(new MonsterIdleState());
            m.Target = null;
        }
    }

    private void ShowRestartUI()
    {
        Cursor.visible = true;
        Panelroot.SetActive(true);
    }

    private async UniTask RunSingleWaveAsync(WaveData wave, CancellationToken ct)
    {
        State = WaveState.Preparing;
        await UniTask.Delay(System.TimeSpan.FromSeconds(wave.waveDelay), cancellationToken: ct);

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[Wave] 스폰 포인트가 없습니다");
            return;
        }

        State = WaveState.Spawning;
        OnWaveStarted?.Invoke(CurrentWaveIndex);

        int spIdx = 0;
        foreach (var entry in wave.spawnEntries)
        {
            for (int n = 0; n < entry.count; n++)
            {
                SpawnOne(entry.monster, spawnPoints[spIdx % spawnPoints.Length].transform);
                spIdx++;
                if (wave.spawnDuration > 0)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(wave.spawnDuration), cancellationToken: ct);
            }
        }

        State = WaveState.InProgress;
        await UniTask.WaitUntil(() => AliveCount <= 0, cancellationToken: ct);

        State = WaveState.Cleared;
        OnWaveCleared?.Invoke(CurrentWaveIndex);
    }


    private async UniTask RunWavesAsync(CancellationToken ct)
    {
        for (int i = 0; i < waves.Length; i++)
        {
            CurrentWaveIndex = i;
            await RunSingleWaveAsync(waves[i], ct);

            if (waves[i].bossPrefabOverride != null)
            {
                State = WaveState.Rest;
                shopUI.Open();
                await UniTask.WaitUntil(() => State == WaveState.Preparing, cancellationToken: ct);
                await RunBossWaveAsync(waves[i].bossPrefabOverride, ct);
            }

            if (i < waves.Length - 1)
            {
                State = WaveState.Rest;
                shopUI.Open();
                await UniTask.WaitUntil(() => State == WaveState.Preparing, cancellationToken: ct);
            }
        }

        State = WaveState.AllCleared;
        OnAllWavesCleared?.Invoke();
    }

    private async UniTask RunBossWaveAsync(GameObject bossPrefab, CancellationToken ct)
    {
        AudioManager.Instance.PlayBGM(AudioManager.Instance.BossBGM);
        State = WaveState.InProgress;
        var boss = Instantiate(bossPrefab, spawnPoints[0].transform.position, Quaternion.identity);
        var bossanim = boss.GetComponent<Animator>();
        bossanim.SetTrigger("Appear");
        var bossHealth = boss.GetComponent<BossHealth>();

        var bossHpBar = FindAnyObjectByType<BossHpBar>();
        bossHpBar.Setup(bossHealth);

        bool bossDead = false;
        bossHealth.OnDeath += () =>
        {
            bossDead = true;
            ElapsedTime = Time.time - startTime;
        };

        await UniTask.WaitUntil(() => bossDead, cancellationToken: ct);
    }

    public void EndRest()
    {
        if (State != WaveState.Rest) return;
        State = WaveState.Preparing;  // WaitUntil 해제 → 다음 웨이브 진행
    }

    [ContextMenu("DEBUG: Kill All Monsters")]
    private void DebugKillAll()
    {
        var healths = FindObjectsByType<MonsterHealth>(FindObjectsSortMode.None);
        foreach (var h in healths)
        {
            h.TakeDamage(9999);
        }
    }
}
