using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PotionHud : MonoBehaviour
{
    [SerializeField] private PotionSystem potion;
    [SerializeField] private TMP_Text potionLabel;
    [SerializeField] private Image potionIcon;      // 쿨타임 fill 이미지

    private void OnEnable()
    {
        potion.OnPotionChanged += Refresh;
        Refresh(potion.CurrentPotions, potion.MaxPotions);
    }

    private void OnDisable()
    {
        potion.OnPotionChanged -= Refresh;
    }

    private void Update()
    {
        potionIcon.fillAmount = potion.CooldownProgress;
    }

    private void Refresh(int current, int max)
    {
        potionLabel.text = $"{current} / {max}";
    }
}