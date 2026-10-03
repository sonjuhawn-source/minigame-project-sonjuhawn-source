using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileEditUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField]
    private GameObject profileEditPanel;

    [SerializeField]
    private GameObject createProfilePanel;

    [SerializeField]
    private GameObject editProfilePanel;

    [Header("Create Profile")]
    [SerializeField]
    private TMP_InputField createNicknameInput;

    [SerializeField]
    private Button createButton;

    [SerializeField]
    private TextMeshProUGUI createErrorText;

    [Header("Edit Profile")]
    [SerializeField]
    private TextMeshProUGUI currentNicknameText;

    [SerializeField]
    private TMP_InputField editNicknameInput;

    [SerializeField]
    private Button updateButton;

    [SerializeField]
    private Button closeEditButton;

    [SerializeField]
    private TextMeshProUGUI editErrorText;

    [Header("References")]
    [Tooltip("선택 사항 — 연결하면 닉네임 변경 직후 표시를 갱신한다")]
    [SerializeField]
    private ProfileUI profileUI;

    private void Start()
    {
        createButton.onClick.AddListener(() => OnCreateButtonClicked().Forget());
        updateButton.onClick.AddListener(() => OnUpdateButtonClicked().Forget());
        closeEditButton.onClick.AddListener(OnCloseEditButtonClicked);

        CloseEditPanel();
    }

    // 이 스크립트가 profileEditPanel 자신에 붙어 있으면 SetActive(false)가
    // 컴포넌트까지 꺼버려 버튼 이벤트를 받지 못한다. 그래서 자식만 닫는다.
    private void CloseEditPanel()
    {
        createProfilePanel.SetActive(false);
        editProfilePanel.SetActive(false);

        if (profileEditPanel != gameObject)
            profileEditPanel.SetActive(false);
    }

    // 버튼 OnClick에 연결하는 진입점 — UniTaskVoid는 Inspector 목록에 뜨지 않는다
    public void OpenProfileEdit()
    {
        OpenProfileEditPanelAsync().Forget();
    }

    public async UniTaskVoid OpenProfileEditPanelAsync()
    {
        profileEditPanel.SetActive(true);
        var (profile, _) = await ProfileManager.Instance.LoadProfileAsync();

        // 조회를 기다리는 동안 패널이 파괴됐을 수 있다
        if (this == null || profileEditPanel == null)
            return;

        if (profile != null)
        {
            ShowEditProfile(profile);
        }
        else
        {
            ShowCreateProfile();
        }
    }

    private void ShowCreateProfile()
    {
        createProfilePanel.SetActive(true);
        editProfilePanel.SetActive(false);
        createNicknameInput.text = "";
        createErrorText.text = "";
    }

    private void ShowEditProfile(UserProfile profile)
    {
        createProfilePanel.SetActive(false);
        editProfilePanel.SetActive(true);

        currentNicknameText.text = $"현재 닉네임: {profile.nickname}";
        editNicknameInput.text = profile.nickname;
        editErrorText.text = "";
    }

    private async UniTaskVoid OnCreateButtonClicked()
    {
        string nickname = createNicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            createErrorText.text = "닉네임을 입력하세요";
            createErrorText.color = Color.red;
            return;
        }

        createButton.interactable = false;

        var (success, error) = await ProfileManager.Instance.SaveProfileAsync(nickname);
        if (success)
        {
            createErrorText.text = "프로필 생성 완료";
            createErrorText.color = Color.green;

            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());
            CloseEditPanel();
            RefreshProfileDisplay();
        }
        else
        {
            createErrorText.text = error;
            createErrorText.color = Color.red;
        }

        createButton.interactable = true;
    }

    private async UniTaskVoid OnUpdateButtonClicked()
    {
        string nickname = editNicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            editErrorText.text = "닉네임을 입력하세요";
            editErrorText.color = Color.red;
            return;
        }

        updateButton.interactable = false;

        var (success, error) = await ProfileManager.Instance.UpdateNicknameAsync(nickname);
        if (success)
        {
            editErrorText.text = "수정 완료";
            editErrorText.color = Color.green;
            currentNicknameText.text = $"현재 닉네임: {nickname}";

            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());
            RefreshProfileDisplay();
        }
        else
        {
            editErrorText.text = error;
            editErrorText.color = Color.red;
        }

        updateButton.interactable = true;
    }

    private void OnCloseEditButtonClicked()
    {
        CloseEditPanel();
    }

    // profileUI는 선택 사항이라 연결되지 않았으면 건너뛴다
    private void RefreshProfileDisplay()
    {
        if (profileUI != null)
            profileUI.UpdateProfileUIAsync().Forget();
    }
}
