using Cysharp.Threading.Tasks;
using Firebase.Auth;
using System;
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    private static AuthManager instance;
    public static AuthManager Instance => instance;

    private FirebaseAuth auth;
    private FirebaseUser currentUser;

    private bool isInitialized = false;
    private bool lastNotifiedSignedIn = false;

    public FirebaseUser CurrentUser => currentUser;
    public bool IsLoggedIn => currentUser != null;
    public string UserId => currentUser?.UserId ?? string.Empty;
    public bool IsInitalized => isInitialized;

    public event Action<bool> LoginStateChagned;

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
        bool isReady = await FirebaseInitializer.Instance.WaitForInitializationAsync();
        if (!isReady)
        {
            Debug.LogError("[Auth] 파이어 베이스 초기화 실패 Auth 초기화 불가");
            return;
        }

        auth = FirebaseInitializer.Instance.Auth;
        auth.StateChanged += OnAutoStateChanged;

        currentUser = auth.CurrentUser;
        Debug.Log(currentUser != null ? "[Auth] 이미 로그인 됨" : "[Auth] 로그인 필요");

        isInitialized = true;
        NotifyLoginState();
    }

    private void OnAutoStateChanged(object sender, EventArgs eventArgs)
    {
        NotifyLoginState();
    }

    private void NotifyLoginState()
    {
        bool signedIn = IsLoggedIn;
        if (signedIn == lastNotifiedSignedIn)
            return;

        lastNotifiedSignedIn = signedIn;
        Debug.Log(signedIn ? "[Auth] 로그인 상태" : "[Auth] 로그아웃 상태");
        LoginStateChagned?.Invoke(signedIn);
    }
    public async UniTask<(bool success, string error)> SignInAnonymouslyAsync()
    {
        try
        {
            AuthResult result = await auth.SignInAnonymouslyAsync();
            currentUser = result.User;

            Debug.Log("[Auth] 익명 로그인 성공");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.Log($"[Auth] 익명 로그인 실패 {ex.Message}");
            return (false, ParseFirebaseError(ex.Message));
        }
    }


    public async UniTask<(bool success, string error)> CreateUserWithEmailAsync(string email, string password)
    {
        try
        {
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            currentUser = result.User;
            NotifyLoginState();

            Debug.Log("[Auth] 회원 가입 성공");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.Log($"[Auth] 회원 가입 실패 {ex.Message}");
            return (false, ParseFirebaseError(ex.Message));
        }
    }
    public async UniTask<(bool success, string error)> SignInUserWithEmailAsync(string email, string password)
    {
        try
        {
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(email, password);
            currentUser = result.User;
            NotifyLoginState();

            Debug.Log("[Auth] 로그인 성공");
            return (true, null);
        }
        catch (Exception ex)
        {
            Debug.Log($"[Auth] 로그인 실패 {ex.Message}");
            return (false, ParseFirebaseError(ex.Message));
        }
    }

    public void SignOut()
    {
        if(auth != null && currentUser != null)
        {
            Debug.Log("[Auth] 로그아웃");
            auth.SignOut();
            currentUser = null;
            NotifyLoginState();
        }
    }

    private string ParseFirebaseError(string error)
    {
        Debug.LogWarning($"[Auth] Firebase 에러 원문: {error}");

        // 오프라인이면 Firebase가 돌려주는 메시지와 무관하게 네트워크 문제로 안내한다.
        // 초기화는 로컬 설정만 읽어 성공하므로, 단절은 이 단계에서야 드러난다.
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return "네트워크 연결을 확인해주세요.";
        }

        string lower = error.ToLowerInvariant();

        if (lower.Contains("already in use") || lower.Contains("email-already"))
        {
            return "이미 사용 중인 이메일입니다.";
        }
        if (lower.Contains("at least 6") || lower.Contains("weak") || lower.Contains("password is invalid"))
        {
            return "비밀번호는 6자 이상이어야 합니다.";
        }
        if (lower.Contains("badly formatted") || lower.Contains("invalid-email"))
        {
            return "이메일 형식이 올바르지 않습니다.";
        }
        if (lower.Contains("network") || lower.Contains("timeout") || lower.Contains("unreachable")
            || lower.Contains("connection"))
        {
            return "네트워크 연결을 확인해주세요.";
        }

        return "이메일 또는 비밀번호를 확인해주세요.";
    }
}
