using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// One stall's stock gauge in the Kanban panel: a vertical fill bar (Image.fillAmount — the exact
// "genuine UI-only capability" the World-Attached NPC UI convention calls out as worth
// escalating to Canvas for) plus a draggable reorder-point marker the player positions along it.
// KanbanBuilderSystem owns the simulation entirely; this component only reports the marker's
// position back as a 0..1 ratio and renders whatever stock ratio/events it's told to show —
// same "system owns the truth, UI just reflects it" split as RoutineCardUI/RoutineSlotUI.
[RequireComponent(typeof(RectTransform))]
public class KanbanStallGaugeUI : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("UI References")]
    [SerializeField] private RectTransform track;          // the gauge bar — drag axis is its long dimension
    [SerializeField] private RectTransform thresholdMarker; // draggable reorder-point pin
    [SerializeField] private Image stockFillImage;          // fillAmount = live stock ratio during simulation
    [SerializeField] private Image markerColorImage;        // tints when the marker sits above the wasteful line
    [SerializeField] private TextMeshProUGUI stallNameLabel;

    [Header("Readouts — closes the \"what am I even looking at\" gap")]
    [SerializeField] private TextMeshProUGUI thresholdReadout; // e.g. "Reorder at 42%", updates live while dragging
    [SerializeField] private TextMeshProUGUI stockReadout;     // e.g. "78%", live stock level during simulation

    [Header("Event Callout — narrates what just happened, since silent bar motion reads as noise")]
    [SerializeField] private TextMeshProUGUI eventCalloutText;
    [SerializeField] private float calloutDuration = 1.5f;

    [Header("Failure Outline — shown after a run if this stall didn't pass")]
    [SerializeField] private GameObject failureOutline;

    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private Color wastefulMarkerColor = Color.yellow;
    [SerializeField] private Color normalMarkerColor = Color.white;

    [Tooltip("Flip if dragging feels backwards after rotating the gauge to make it vertical.")]
    [SerializeField] private bool invertDrag = false;

    public float ThresholdRatio { get; private set; } = 0.5f;
    private float wastefulThresholdRatio = 1f;
    private Coroutine calloutRoutine;

    public void Initialize(string stallName, float wastefulRatio)
    {
        if (stallNameLabel != null) stallNameLabel.text = stallName;
        wastefulThresholdRatio = wastefulRatio;
        SetFailureOutline(false);
        if (eventCalloutText != null) eventCalloutText.gameObject.SetActive(false);
    }

    public void SetThresholdRatio(float ratio)
    {
        ThresholdRatio = Mathf.Clamp01(ratio);
        PositionMarker();
        UpdateMarkerColor();
        if (thresholdReadout != null)
            thresholdReadout.text = $"Reorder at {Mathf.RoundToInt(ThresholdRatio * 100f)}%";
    }

    public void OnBeginDrag(PointerEventData eventData) => OnDrag(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        if (KanbanBuilderSystem.Instance != null && !KanbanBuilderSystem.Instance.CanConfigure)
            return;
        if (track == null) return;

        // Drag runs along whichever local axis is actually the bar's LONG dimension — width or
        // height — rather than assuming height. Rotating the gauge's Transform 90° to turn a
        // horizontal source sprite into a vertical bar changes which axis reads as "long" on
        // screen without necessarily swapping the RectTransform's own Width/Height numbers to
        // match, so track.rect.height can still be the small original value even once the bar
        // visually reads as tall. TransformPoint then carries those two local extremes through
        // whatever rotation is actually applied, so this stays correct either way.
        Vector3 localStart, localEnd;
        if (track.rect.width >= track.rect.height)
        {
            localStart = new Vector3(track.rect.xMin, track.rect.center.y, 0f);
            localEnd = new Vector3(track.rect.xMax, track.rect.center.y, 0f);
        }
        else
        {
            localStart = new Vector3(track.rect.center.x, track.rect.yMin, 0f);
            localEnd = new Vector3(track.rect.center.x, track.rect.yMax, 0f);
        }

        Camera cam = eventData.pressEventCamera;
        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(cam, track.TransformPoint(localStart));
        Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(cam, track.TransformPoint(localEnd));

        Vector2 axis = endScreen - startScreen;
        float axisLengthSq = axis.sqrMagnitude;
        if (axisLengthSq < 0.0001f) return;

        float normalized = Vector2.Dot(eventData.position - startScreen, axis) / axisLengthSq;
        if (invertDrag) normalized = 1f - normalized;
        SetThresholdRatio(normalized);
    }

    private void PositionMarker()
    {
        if (track == null || thresholdMarker == null) return;

        // Same long-axis reasoning as OnDrag — move the marker along whichever of Width/Height
        // is actually the bar's long dimension, so it visibly tracks ThresholdRatio regardless of
        // whether the RectTransform's numeric Width/Height were swapped when the gauge was
        // rotated to read as vertical.
        if (track.rect.width >= track.rect.height)
        {
            float x = Mathf.Lerp(track.rect.xMin, track.rect.xMax, ThresholdRatio);
            thresholdMarker.anchoredPosition = new Vector2(x, thresholdMarker.anchoredPosition.y);
        }
        else
        {
            float y = Mathf.Lerp(track.rect.yMin, track.rect.yMax, ThresholdRatio);
            thresholdMarker.anchoredPosition = new Vector2(thresholdMarker.anchoredPosition.x, y);
        }
    }

    private void UpdateMarkerColor()
    {
        if (markerColorImage == null) return;
        markerColorImage.color = ThresholdRatio > wastefulThresholdRatio ? wastefulMarkerColor : normalMarkerColor;
    }

    // Called every simulated frame by KanbanBuilderSystem while the day plays out — same
    // per-frame color-lerp-toward-a-problem-color convention as BridgePlank.UpdateStressVisual.
    // Always shows the honest live ratio rather than force-coloring red for a "wasteful" flag
    // alone — that used to make a stall sitting at 90% full look visually identical to one
    // actually about to run dry. A real stockout still reads as red on its own, since the lerp
    // naturally approaches dangerColor as ratio nears 0; SetFailureOutline is the dedicated
    // signal for a wasteful failure, which can't be read from stock level at all.
    public void SetStockRatio(float ratio)
    {
        if (stockFillImage == null) return;
        stockFillImage.fillAmount = Mathf.Clamp01(ratio);
        stockFillImage.color = Color.Lerp(dangerColor, healthyColor, ratio);

        if (stockReadout != null)
            stockReadout.text = $"{Mathf.RoundToInt(Mathf.Clamp01(ratio) * 100f)}%";
    }

    // Fired by KanbanBuilderSystem the instant a reorder triggers, a delivery lands, or a stall
    // actually hits 0 — narrates the simulation instead of leaving it as silent bar motion.
    public void ShowEventCallout(string message)
    {
        if (eventCalloutText == null) return;
        if (calloutRoutine != null) StopCoroutine(calloutRoutine);
        calloutRoutine = StartCoroutine(CalloutRoutine(message));
    }

    private IEnumerator CalloutRoutine(string message)
    {
        eventCalloutText.text = message;
        eventCalloutText.gameObject.SetActive(true);
        yield return new WaitForSeconds(calloutDuration);
        eventCalloutText.gameObject.SetActive(false);
    }

    // Set once at the end of a run — a dedicated visual separate from the fill color, since a
    // wasteful failure can look nearly full and a color-only signal there would be missed.
    public void SetFailureOutline(bool visible)
    {
        if (failureOutline != null) failureOutline.SetActive(visible);
    }
}
