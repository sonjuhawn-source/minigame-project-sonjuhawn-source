using Cysharp.Threading.Tasks;
using UnityEngine;

public class ClearTimeManager : MonoBehaviour
{
    public static ClearTimeManager instance;
    public static ClearTimeManager Instance => instance;

    private void Awake() 
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private async UniTaskVoid Start()
    {
        if (!await FirebaseInitializer.Instance.WaitForInitializationAsync())
        {
            Debug.LogError("[ClearTime] Firebase 초기화 실패");
            return;
        }

        await UniTask.WaitUntil(() => AuthManager.Instance.IsInitalized);
        await UniTask.WaitUntil(() => ProfileManager.Instance.IsInitialized);

        Debug.Log("[ClearTime] 초기화 완료");
    }

    public async UniTask SubmitClearTimeAsync(float clearTime, int kills, int goldSpent)
    {
        if (!AuthManager.Instance.IsLoggedIn) return;

        bool isAnonymous = AuthManager.Instance.CurrentUser.IsAnonymous;

        string displayName = isAnonymous ? "Anonymous" : await ResolveNicknameAsync();

        await LeaderboardManager.Instance.SaveToLeaderboardAsync(clearTime, displayName);
    }

    // 자동 로그인으로 들어오면 CachedProfile이 비어 있다. 저장 직전에 한 번 더 확인한다.
    private async UniTask<string> ResolveNicknameAsync()
    {
        string nickname = ProfileManager.Instance.CachedProfile?.nickname;

        if (string.IsNullOrWhiteSpace(nickname))
        {
            var (profile, error) = await ProfileManager.Instance.LoadProfileAsync();
            if (!string.IsNullOrEmpty(error))
                Debug.LogWarning($"[ClearTime] 프로필 로드 실패: {error}");
            nickname = profile?.nickname;
        }

        if (!string.IsNullOrWhiteSpace(nickname))
            return nickname;

        // 닉네임을 끝내 못 구하면 이메일 노출 대신 Player로 표시한다
        Debug.LogWarning("[ClearTime] 닉네임을 찾지 못해 기본 표시명으로 저장합니다");
        return "Player";
    }
}
