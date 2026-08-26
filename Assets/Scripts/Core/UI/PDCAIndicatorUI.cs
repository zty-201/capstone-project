using UnityEngine;
using TMPro;

public class PDCAIndicatorUI : MonoBehaviour
{
    [System.Serializable]
    public class PhaseLabel
    {
        public PDCAPhase phase;
        public TextMeshProUGUI label;
    }

    [Header("Labels — Plan / Do / Check")]
    [SerializeField] private PhaseLabel[] phaseLabels;

    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.35f);

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    private void OnEnable() => EventBus.OnPDCAPhaseChanged += HandlePhaseChanged;
    private void OnDisable() => EventBus.OnPDCAPhaseChanged -= HandlePhaseChanged;

    private void HandlePhaseChanged(PDCAPhase phase)
    {
        if (phase == PDCAPhase.None)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        canvasGroup.alpha = 1f;
        foreach (var entry in phaseLabels)
            entry.label.color = entry.phase == phase ? activeColor : inactiveColor;
    }
}
