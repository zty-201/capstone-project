using UnityEngine;

// Mission 4's trivial minigame: over a short simulated market day, every MarketStall's stock
// drains on its own; the player taps a stall to restock it back to full whenever they notice it
// running low. The day always resolves once dayDuration elapses, regardless of how many stalls
// sat empty along the way — same "trivial always completes, just badly" shape every other
// mission's trivial path follows (see WastePickupSystem, WellPatchSite).
public class MarketStallTrivialSystem : MonoBehaviour
{
    [SerializeField] private int missionID = 4;
    [SerializeField] private MarketStall[] stalls;
    [SerializeField] private float dayDuration = 30f;

    private float dayTimer;
    private bool dayRunning;

    // Subscribed in Awake/OnDestroy, not OnEnable/OnDisable: this container is disabled by
    // MinigameActivator once the mission completes, and an OnEnable/OnDisable subscription would
    // already be torn down by the time a later Stage Gate review request needs to reach it.
    private void Awake() => EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    private void OnEnable() => ResetDay();

    private void Update()
    {
        if (!dayRunning) return;
        // Same pause-outside-Exploration gating as TrashSpawner — a stall shouldn't drain while
        // the player is mid-dialogue or inside another mission's minigame.
        if (GameManager.Instance.StateManager.CurrentStateType != GameStateType.Exploration) return;

        dayTimer += Time.deltaTime;
        foreach (var stall in stalls)
            if (stall != null) stall.TickDeplete(Time.deltaTime);

        if (dayTimer >= dayDuration)
        {
            dayRunning = false;
            EventBus.RaiseMissionCompleted(missionID, false);
        }
    }

    private void ResetDay()
    {
        dayTimer = 0f;
        dayRunning = true;
        foreach (var stall in stalls)
            if (stall != null) stall.ResetStall();
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        ResetDay();
    }
}
