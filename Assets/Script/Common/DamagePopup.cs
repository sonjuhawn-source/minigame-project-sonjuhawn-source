using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float riseSpeed = 50f;

    private RectTransform rectTransform;
    private Vector3 worldPos;

    public void Init(int damage, Vector3 position)
    {
        rectTransform = GetComponent<RectTransform>();
        worldPos = position + Vector3.up * 1.5f;
        damageText.text = damage.ToString();
        AnimateAndDestroyAsync().Forget();
    }

    private async UniTaskVoid AnimateAndDestroyAsync()
    {
        // 오브젝트가 파괴되면 대기를 취소한다 — 파괴된 RectTransform 접근 방지
        var ct = this.GetCancellationTokenOnDestroy();
        float elapsed = 0f;
        Color color = damageText.color;

        while (elapsed < lifetime)
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;

            elapsed += Time.deltaTime;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            rectTransform.position = screenPos + Vector3.up * (riseSpeed * elapsed);
            color.a = 1f - (elapsed / lifetime);
            damageText.color = color;

            if (await UniTask.Yield(ct).SuppressCancellationThrow())
                return;
        }
        Destroy(gameObject);
    }
}