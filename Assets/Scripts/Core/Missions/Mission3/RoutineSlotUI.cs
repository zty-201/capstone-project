using UnityEngine;
using UnityEngine.EventSystems;

// One fixed position in the routine's ordered row. Cards reparent into a slot's transform when
// placed there (see FarmRoutineSystem.PlaceCardInSlot), so a slot's own child count/position is
// what defines "what order is the player proposing" — FarmRoutineSystem reads it back via
// CurrentCard rather than tracking order in a separate array that could drift out of sync with
// what's actually on screen.
public class RoutineSlotUI : MonoBehaviour, IDropHandler
{
    [Tooltip("Position in the routine, left-to-right — 0 is first in the day.")]
    public int slotIndex;

    public RoutineCardUI CurrentCard { get; set; }

    public void OnDrop(PointerEventData eventData)
    {
        RoutineCardUI dropped = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<RoutineCardUI>()
            : null;
        if (dropped == null) return;

        if (FarmRoutineSystem.Instance != null)
            FarmRoutineSystem.Instance.HandleCardDropped(dropped, this);
    }
}
