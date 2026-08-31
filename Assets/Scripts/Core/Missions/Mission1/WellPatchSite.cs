using UnityEngine;

// Trivial-path completion for Mission 1: plain walk-up-and-click, same shape as every other
// resolution site (WastePiece, RiverInteractable, the old WellVisual) — gated on actually
// carrying a Brick (fetched via BrickPickup), so a click before the fetch step is a no-op.
// Supersedes the earlier drag-and-drop BrickDraggable/WellPatchGate pair: dragging was the
// only interaction of its kind in the game, and read as tonally distorted next to the rest
// of the game's click-to-resolve minigames.
public class WellPatchSite : MonoBehaviour, IInteractable
{
    [SerializeField] private int missionID = 1;
    [SerializeField] private ItemData brickItem;
    [SerializeField] private GameObject patchedWellVisual;
    [SerializeField] private BrickPickup brickPickup;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    // Subscribed in Awake/OnDestroy, not OnEnable/OnDisable: this object lives inside
    // Container_Trivial_M1, which MinigameActivator disables right after this path's mission
    // completes, so an OnEnable/OnDisable subscription would already be gone by the time a
    // review request — which can only happen after the mission is complete — needs to reach it.
    private void Awake() => EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    public void Interact()
    {
        // No-op until the brick's actually been fetched — the objective HUD line already
        // tells the player to go find one first.
        if (!InventorySystem.Instance.TryRemoveItem(brickItem, 1)) return;

        if (patchedWellVisual != null) patchedWellVisual.SetActive(true);
        EventBus.RaiseMissionCompleted(missionID, false);
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        if (patchedWellVisual != null) patchedWellVisual.SetActive(false);
        brickPickup?.ResetPickup();
    }
}
