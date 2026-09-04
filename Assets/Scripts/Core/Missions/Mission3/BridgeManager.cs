using UnityEngine;

// Reveals the permanent bridge visual and unlocks the far bank once Mission 5 resolves — mirrors
// RiverManager (Mission 2): the minigame containers themselves get torn down by MinigameActivator
// right after completion, so anything meant to persist afterward (the standing bridge, access to
// the far side) has to live outside them. Unlike RiverManager, the two outcomes actually look
// different here (a rickety rope crossing vs. a properly braced bridge), so this branches on
// wasOptimal for the visual instead of showing the same art either way.
public class BridgeManager : MonoBehaviour
{
    [SerializeField] private int missionID = 5;
    [SerializeField] private GameObject brokenBridgeVisual;
    [SerializeField] private GameObject lashedBridgeVisual;
    [SerializeField] private GameObject bracedBridgeVisual;

    [Header("Far Bank Access")]
    [SerializeField] private PathfindingSystem pathfindingSystem;
    // World positions of every grid cell spanning the bridge/leading onto the far bank —
    // pre-authored unwalkable in the collision tilemap, flipped walkable here.
    [SerializeField] private Vector3[] unlockedCells;

    private void OnEnable()
    {
        EventBus.OnMissionCompleted += HandleMissionCompleted;
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDisable()
    {
        EventBus.OnMissionCompleted -= HandleMissionCompleted;
        EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;
    }

    private void HandleMissionCompleted(int id, bool wasOptimal)
    {
        if (id != missionID) return;
        if (brokenBridgeVisual != null) brokenBridgeVisual.SetActive(false);
        if (lashedBridgeVisual != null) lashedBridgeVisual.SetActive(!wasOptimal);
        if (bracedBridgeVisual != null) bracedBridgeVisual.SetActive(wasOptimal);

        // Both paths physically get the player across, so both unlock the far bank — and this
        // is deliberately one-way (not undone in HandleMissionsNeedReview below): the player may
        // already be exploring the far side when a trivial outcome gets flagged for redo, and
        // re-locking the only way back would strand them there.
        foreach (var cell in unlockedCells)
            pathfindingSystem.SetWalkable(cell, true);
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        if (brokenBridgeVisual != null) brokenBridgeVisual.SetActive(true);
        if (lashedBridgeVisual != null) lashedBridgeVisual.SetActive(false);
        if (bracedBridgeVisual != null) bracedBridgeVisual.SetActive(false);
    }
}
