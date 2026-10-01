using System;
using UnityEngine;

public class MonsterHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private MonsterData data;
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private Transform damageCanvas;

    private int currentHp;

    public int CurrentHp => currentHp;
    public int MaxHp => data != null ? data.maxHp : 0;
    public bool IsDead => currentHp <= 0;

    public event Action<int, int> OnHpChanged;
    public event Action OnDeath;
    public event Action OnDamaged;

    private void Awake()
    {
        if (data != null) currentHp = data.maxHp;

        if (damageCanvas == null)
        {
            // 씬에 PopUpCanvas가 없거나 비활성이면 Find가 null을 돌려준다
            var popUpCanvas = GameObject.Find("PopUpCanvas");
            damageCanvas = popUpCanvas != null
                ? popUpCanvas.transform
                : FindAnyObjectByType<Canvas>()?.transform;
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) 
            return;

        currentHp = Mathf.Max(currentHp - amount, 0);
        OnHpChanged?.Invoke(currentHp, MaxHp);

        if (currentHp == 0)
            OnDeath?.Invoke();
        else
            OnDamaged?.Invoke();

        if (damagePopupPrefab == null || damageCanvas == null)
            return;

        var popup = Instantiate(damagePopupPrefab, damageCanvas);
        popup.GetComponent<DamagePopup>().Init(amount, transform.position);
    }

    public void Initialize(MonsterData d)
    {
        data = d;
        currentHp = d.maxHp;
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }
}