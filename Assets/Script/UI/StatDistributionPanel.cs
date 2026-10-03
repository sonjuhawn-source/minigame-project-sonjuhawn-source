using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatDistributionPanel : MonoBehaviour
{
    // === 시스템 참조 ===
    [SerializeField] private GameObject panelRoot;   
    [SerializeField] private ExperienceSystem exp;
    [SerializeField] private PlayerStats stats;

    // === UI 참조 ===
    [SerializeField] private TMP_Text pointsLabel;
    [SerializeField] private TMP_Text strengthValue;
    [SerializeField] private TMP_Text agilityValue;
    [SerializeField] private TMP_Text vitalityValue;
    [SerializeField] private TMP_Text staminaValue;
    [SerializeField] private Button confirmButton;

    [SerializeField] private AudioClip statUpgradeClip;

    // === 이벤트 구독/해제 ===
    private void OnEnable()
    {
        exp.OnLevelUp += HandleLevelUp;
        stats.OnStatChanged += Refresh;
    }

    private void OnDisable()
    {
        exp.OnLevelUp -= HandleLevelUp;
        stats.OnStatChanged -= Refresh;
    }

    // OnLevelUp은 레벨당 한 번 오므로 한 프레임에 2레벨이 오르면 두 번 불린다.
    // 그때 Pause를 두 번 쌓으면 확인(Resume 1회)으로 pauseCount가 0에 닿지 못해
    // 패널만 닫히고 timeScale이 0에 묶인다. 열려 있으면 포인트만 갱신한다.
    private void HandleLevelUp(int newLevel)
    {
        if (!panelRoot.activeSelf)
        {
            panelRoot.SetActive(true);
            PauseManager.Instance.Pause();
        }
        Refresh();
    }

    public void OnPlus(int statTypeInt)
    {
        if (exp.SpendPoint((StatType)statTypeInt))
        {
            AudioManager.Instance.PlaySFX(statUpgradeClip);
            Refresh();
        }
    }

    public void OnConfirm()
    {
        PauseManager.Instance.Resume();
        panelRoot.SetActive(false);
    }

    private void Refresh()
    {
        pointsLabel.text = $"Point: {exp.PendingPoints}";
        strengthValue.text = $"{stats.Strength}";
        agilityValue.text = $"{stats.Agility}";
        vitalityValue.text = $"{stats.Vitality}";
        staminaValue.text = $"{stats.Stamina}";

        if (exp.PendingPoints == 0)
            confirmButton.interactable = true;
        else
            confirmButton.interactable = false;

    }
}