using UnityEngine;

// 겹친 UI를 bool 하나로 관리하면 상점만 닫아도 스탯창이 열린 채 게임이 재개된다.
// 카운터로 두고 마지막 UI가 닫힐 때(pauseCount == 0)만 timeScale을 되돌린다.
// 호출: ShopSystem · StatDistributionPanel · PauseUI
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    private int pauseCount = 0;

    public int PauseCount => pauseCount;

    private void Awake() => Instance = this;

    public void Pause()
    {
        pauseCount++;
        Time.timeScale = 0f;
        ApplyCursor();
    }
    public void Resume()
    {
        pauseCount = Mathf.Max(0, pauseCount - 1);
        if (pauseCount == 0) Time.timeScale = 1f;
        ApplyCursor();
    }
    private void ApplyCursor()
    {
        bool uiOpen = pauseCount > 0;
        Cursor.visible = uiOpen;
        Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
    }
}