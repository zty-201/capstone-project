using UnityEngine;

public class WastePickupSystem : MonoBehaviour
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private int wasteCount;

    private int remaining;

    // Subscribed in Awake/OnDestroy, not OnEnable/OnDisable: this object lives inside a
    // container MinigameActivator disables right after this path's mission completes, so an
    // OnEnable/OnDisable subscription would already be gone by the time a stage fail — which
    // can only happen after the mission is complete — needs to reach it.
    private void Awake() => EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    private void OnEnable()
    {
        remaining = wasteCount;
    }

    public void OnWasteRemoved()
    {
        remaining--;
        if (remaining <= 0)
            EventBus.RaiseMissionCompleted(missionID, false);
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        remaining = wasteCount;
        foreach (var piece in GetComponentsInChildren<WastePiece>(true))
            piece.ResetPiece();
    }
}
