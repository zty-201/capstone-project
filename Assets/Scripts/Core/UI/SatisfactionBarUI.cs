using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SatisfactionBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI valueText;

    private void OnEnable() => EventBus.OnSatisfactionChanged += HandleSatisfactionChanged;
    private void OnDisable() => EventBus.OnSatisfactionChanged -= HandleSatisfactionChanged;

    private void HandleSatisfactionChanged(int newValue)
    {
        fillImage.fillAmount = (float)newValue / TownSatisfactionSystem.Instance.MaxSatisfaction;
        if (valueText != null) valueText.text = $"{newValue}/{TownSatisfactionSystem.Instance.MaxSatisfaction}";
    }
}
