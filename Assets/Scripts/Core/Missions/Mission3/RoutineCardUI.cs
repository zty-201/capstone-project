using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// One draggable station card (e.g. "Feed the Animals") — pure background + text, no icon (there
// isn't an obvious icon for an action like this; the richer visual storytelling is left to a
// future CG). Standard Unity UI drag pattern: on begin-drag it detaches to the root Canvas so it
// renders above every slot and isn't clipped by one, then follows the pointer; on end-drag it
// snaps back to wherever it started unless a RoutineSlotUI's OnDrop already reparented it
// somewhere else in the meantime (OnDrop on the target always fires before OnEndDrag on the
// dragged object, per Unity's EventSystem order — see FarmRoutineSystem.HandleCardDropped, which
// does that reparenting).
[RequireComponent(typeof(RectTransform))]
public class RoutineCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Highlight")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightDuration = 0.3f;

    public int StationIndex { get; private set; }

    private RectTransform rect;
    private Canvas rootCanvas;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Color originalBackgroundColor;
    private Coroutine highlightRoutine;

    private void Awake()
    {
        rect = (RectTransform)transform;
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
        if (background != null) originalBackgroundColor = background.color;
    }

    public void Initialize(int stationIndex, string stationName)
    {
        StationIndex = stationIndex;
        if (label != null) label.text = stationName;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (FarmRoutineSystem.Instance != null && !FarmRoutineSystem.Instance.CanRearrange)
            return;

        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        if (rootCanvas != null) transform.SetParent(rootCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null || transform.parent != rootCanvas.transform) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)rootCanvas.transform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            rect.localPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        // Still parented to the root canvas means no RoutineSlotUI.OnDrop claimed this card —
        // snap back to where it started instead of leaving it floating at the drop point.
        if (rootCanvas != null && transform.parent == rootCanvas.transform)
        {
            transform.SetParent(originalParent, false);
            transform.SetSiblingIndex(originalSiblingIndex);
            ((RectTransform)transform).anchoredPosition = Vector2.zero;
        }
    }

    // Called by FarmRoutineSystem while stepping through a submitted order, as a lightweight
    // stand-in for the design doc's "small CG to show the NPC executing the tasks" — a full
    // animated CG can replace/augment this later without touching the ordering logic.
    public void PlayStepHighlight()
    {
        if (background == null) return;
        if (highlightRoutine != null) StopCoroutine(highlightRoutine);
        highlightRoutine = StartCoroutine(HighlightPulse());
    }

    private IEnumerator HighlightPulse()
    {
        background.color = highlightColor;
        yield return new WaitForSeconds(highlightDuration);
        background.color = originalBackgroundColor;
    }
}
