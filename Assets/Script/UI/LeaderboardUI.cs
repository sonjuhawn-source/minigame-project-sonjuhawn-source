using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Transform entryContainer;  
    [SerializeField] private GameObject entryPrefab;   

    private bool subscribed;

    private async UniTaskVoid Start()
    {
        closeButton.onClick.AddListener(Close);
        leaderboardPanel.SetActive(false);

        // 패널이 파괴되면 대기를 취소한다 — 파괴된 참조 접근 방지
        var ct = this.GetCancellationTokenOnDestroy();
        if (await UniTask.WaitUntil(() => LeaderboardManager.Instance != null && LeaderboardManager.Instance.IsReady,
                cancellationToken: ct).SuppressCancellationThrow())
            return;
        if (await UniTask.WaitUntil(() => AuthManager.Instance != null,
                cancellationToken: ct).SuppressCancellationThrow())
            return;

        AuthManager.Instance.LoginStateChagned += OnLoginStateChanged;
        subscribed = true;

        // 로그인 전에는 읽지 않는다 — leaderboard 규칙이 auth != null
        if (AuthManager.Instance.IsLoggedIn)
            await LoadAndDisplayAsync();
    }

    private void OnDestroy()
    {
        if (subscribed && AuthManager.Instance != null)
            AuthManager.Instance.LoginStateChagned -= OnLoginStateChanged;
    }

    private void OnLoginStateChanged(bool loggedIn)
    {
        // 열려 있는 동안 로그인되면 그 시점에 다시 불러온다
        if (loggedIn && leaderboardPanel.activeSelf)
            LoadAndDisplayAsync().Forget();
    }

    private async UniTaskVoid OpenAsync()
    {
        if (await UniTask.WaitUntil(() => LeaderboardManager.Instance != null && LeaderboardManager.Instance.IsReady,
                cancellationToken: this.GetCancellationTokenOnDestroy()).SuppressCancellationThrow())
            return;
        await LoadAndDisplayAsync();  // 최신 데이터로 갱신
        leaderboardPanel.SetActive(true);
    }

    private async UniTask LoadAndDisplayAsync()
    {
        for (int i = entryContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(entryContainer.GetChild(i).gameObject);
        }

        if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
        {
            if (ToastManager.Instance != null)
                ToastManager.Instance.Show("로그인 후 이용할 수 있습니다");
            return;
        }

        List<LeaderboardEntry> entries = await LeaderboardManager.Instance.LoadLeaderboardAsync();

        for (int i = 0; i < entries.Count; i++)
        {
            GameObject entry = Instantiate(entryPrefab, entryContainer);
            TMP_Text[] texts = entry.GetComponentsInChildren<TMP_Text>();

            int minutes = (int)(entries[i].clearTime / 60);
            int seconds = (int)(entries[i].clearTime % 60);

            texts[0].text = $"{i + 1}.";
            texts[1].text = entries[i].displayName;
            texts[2].text = $"{minutes:00}:{seconds:00}";
        }
    }

    public void Open()
    {
        OpenAsync().Forget();
    }
    private void Close()
    {
        leaderboardPanel.SetActive(false);
    }
}