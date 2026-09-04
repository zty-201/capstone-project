using UnityEngine;

// Trivial-path completion for Mission 5: same shape as WellPatchSite (Mission 1) — a plain
// walk-up-and-click gated on actually carrying Rope (fetched via RopePickup). A lashed rope
// crossing works today but isn't braced for real loads, so it's flagged for a Stage Gate redo
// like every other trivial fix. The visual reveal itself lives in BridgeManager (mirroring
// RiverManager), not here, since this object sits inside Container_Trivial_M5 and gets torn
// down by MinigameActivator right after completion — anything meant to persist afterward can't
// live inside it.
public class BridgeLashPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private int missionID = 5;
    [SerializeField] private ItemData ropeItem;
    [SerializeField] private RopePickup ropePickup;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    // Subscribed in Awake/OnDestroy, not OnEnable/OnDisable: this object lives inside
    // Container_Trivial_M5, which MinigameActivator disables right after this path's mission
    // completes, so an OnEnable/OnDisable subscription would already be gone by the time a
    // review request — which can only happen after the mission is complete — needs to reach it.
    private void Awake() => EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    public void Interact()
    {
        // No-op until the rope's actually been fetched — the objective HUD line already tells
        // the player to go find some first.
        if (!InventorySystem.Instance.TryRemoveItem(ropeItem, 1)) return;

        EventBus.RaiseMissionCompleted(missionID, false);
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        ropePickup?.ResetPickup();
    }
}
