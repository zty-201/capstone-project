using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// One stall's stock gauge in the Kanban panel: a vertical fill bar (Image.fillAmount — the exact
// "genuine UI-only capability" the World-Attached NPC UI convention calls out as worth
// escalating to Canvas for) plus a draggable reorder-point marker the player positions along it.
// KanbanBuilderSystem owns the simulation entirely; this component only reports the marker's
// position back as a 0..1 ratio and renders whatever stock ratio it's told to show each frame —
// same "system owns the truth, UI just reflects it" split as RoutineCardUI/RoutineSlotUI.
[RequireComponent(typeof(RectTransform))]
public class KanbanStallGaugeUI : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("UI References")]
    [SerializeField] private RectTransform track;          // the gauge bar — drag axis is its height
    [SerializeField] private RectTransform thresholdMarker; // draggable reorder-point pin
    [SerializeField] private Image stockFillImage;          // fillAmount = live stock ratio during simulation
    [SerializeField] private Image markerColorImage;        // tints when the marker sits above the wasteful line
    [SerializeField] private TextMeshProUGUI stallNameLabel;

    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private Color wastefulMarkerColor = Color.yellow;
    [SerializeField] private Color normalMarkerColor = Color.white;

    public float ThresholdRatio { get; private set; } = 0.5f;
    private float wastefulThresholdRatio = 1f;

    public void Initialize(string stallName, float wastefulRatio)
    {
        if (stallNameLabel != null) stallNameLabel.text = stallName;
        wastefulThresholdRatio = wastefulRatio;
    }

    public void SetThresholdRatio(float ratio)
    {
        ThresholdRatio = Mathf.Clamp01(ratio);
        PositionMarker();
        UpdateMarkerColor();
    }

    public void OnBeginDrag(PointerEventData eventData) => OnDrag(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        if (KanbanBuilderSystem.Instance != null && !KanbanBuilderSystem.Instance.CanConfigure)
            return;
        if (track == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                track, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            float normalized = Mathf.InverseLerp(-track.rect.height / 2f, track.rect.height / 2f, localPoint.y);
            SetThresholdRatio(normalized);
        }
    }

    private void PositionMarker()
    {
        if (track == null || thresholdMarker == null) return;
        float y = Mathf.Lerp(-track.rect.height / 2f, track.rect.height / 2f, ThresholdRatio);
        thresholdMarker.anchoredPosition = new Vector2(thresholdMarker.anchoredPosition.x, y);
    }

    private void UpdateMarkerColor()
    {
        if (markerColorImage == null) return;
        markerColorImage.color = ThresholdRatio > wastefulThresholdRatio ? wastefulMarkerColor : normalMarkerColor;
    }

    // Called every simulated frame by KanbanBuilderSystem while the day plays out — same
    // per-frame color-lerp-toward-a-problem-color convention as BridgePlank.UpdateStressVisual.
    public void SetStockRatio(float ratio, bool failed)
    {
        if (stockFillImage == null) return;
        stockFillImage.fillAmount = Mathf.Clamp01(ratio);
        stockFillImage.color = failed ? dangerColor : Color.Lerp(dangerColor, healthyColor, ratio);
    }
}
