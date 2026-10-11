using Cysharp.Threading.Tasks;
using Firebase.Database;
using System;
using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    private static ProfileManager instance;
    public static ProfileManager Instance => instance;

    private DatabaseReference databaseRef;
    private DatabaseReference usersRef;

    private UserProfile cachedProfile;
    public UserProfile CachedProfile => cachedProfile;

    private bool isInitialized;
    public bool IsInitialized => isInitialized;

    private void Awake()
    {
        if(instance == null)
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
        if(!await FirebaseInitializer.Instance.WaitForInitializationAsync())
        {
            Debug.LogError("[Profile] Firebase 초기화 실패 — Profile 초기화 불가");
            return;
        }

        databaseRef = FirebaseInitializer.Instance.Database.RootReference;
        usersRef = databaseRef.Child("users");

        isInitialized = true;
        Debug.Log("[Profile] Firebase 초기화 완료");
    }

    public async UniTask<(bool success, string error)> SaveProfileAsync(string nickname)
    {
        if (!AuthManager.Instance.IsLoggedIn)
        {
            return (false, "로그인이 필요합니다");
        }

        string userId = AuthManager.Instance.UserId;
        string email = AuthManager.Instance.CurrentUser.Email ?? "익명";

        try
        {
            UserProfile profile = new UserProfile(nickname, email);
            string json = profile.ToJson();

            await usersRef.Child(userId).SetRawJsonValueAsync(json);
            cachedProfile = profile;

            Debug.Log("[Profile] 프로필 저장 성공");
            return (true, null);
        }
        catch(Exception ex)
        {
            Debug.LogError($"[Profile] 프로필 저장 실패: {ex.Message}");
            return(false, ex.Message);
        }
    }

    public async UniTask<(UserProfile profile, string error)> LoadProfileAsync()
    {
        string userId = AuthManager.Instance.UserId;

        try
        {
            DataSnapshot snapshot = await usersRef.Child(userId).GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.Log("[Profile] 프로필 없음");
                return (null, "프로필이 존재하지 않습니다");
            }

            string json = snapshot.GetRawJsonValue();
            UserProfile profile = UserProfile.FromJson(json);
            cachedProfile = profile;

            Debug.Log($"[Profile] 프로필 로드 성공: {profile.nickname}");
            return (profile, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Profile] 프로필 로드 실패: {ex.Message}");
            return (null, ex.Message);
        }
    }

    public async UniTask<(bool success, string error)> UpdateNicknameAsync(string nickname)
    {
        if (!AuthManager.Instance.IsLoggedIn)
        {
            return (false, "로그인이 필요합니다");
        }

        string userId = AuthManager.Instance.UserId;

        try
        {
            await usersRef.Child(userId).Child("nickname").SetValueAsync(nickname);
            cachedProfile.nickname = nickname;

            Debug.Log("[Profile] 닉네임 수정 성공");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Profile] 닉네임 수정 실패: {ex.Message}");
            return (false, ex.Message);
        }
    }
}

