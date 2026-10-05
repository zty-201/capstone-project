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

    // How close the player has to be for the day to actually progress — without this, the day
    // timer/depletion would run purely off GameStateType.Exploration, which stays true even while
    // the player is off exploring some other part of town entirely. Every other mission's trivial
    // path (Mission 1's fetch quest, Mission 2's tap-the-rubble) waits indefinitely for the player
    // to act instead of running a background clock, so a player who wanders off could otherwise
    // come back to a mission that silently resolved trivially without ever seeing it happen.
    [SerializeField] private float marketRadius = 5f;

    private Transform player;
    private float dayTimer;
    private bool dayRunning;

    // Subscribed in Awake/OnDestroy, not OnEnable/OnDisable: this container is disabled by
    // MinigameActivator once the mission completes, and an OnEnable/OnDisable subscription would
    // already be torn down by the time a later breakdown (review) request needs to reach it.
    private void Awake() => EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    private void OnEnable()
    {
        // Same tag-lookup pattern InteractionIndicator uses — safe to do here regardless of
        // Awake/OnEnable ordering across objects, since it doesn't touch GameManager.Instance.
        if (player == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        ResetDay();
    }

    private void Update()
    {
        if (!dayRunning) return;
        // Same pause-outside-Exploration gating as TrashSpawner — a stall shouldn't drain while
        // the player is mid-dialogue or inside another mission's minigame.
        if (GameManager.Instance.StateManager.CurrentStateType != GameStateType.Exploration) return;
        // The actual fix: also pause while the player is simply somewhere else in town. This is
        // deliberately a pause, not a reset — wandering off doesn't punish or help you, the day
        // just waits for you to come back before continuing.
        if (player != null && Vector2.Distance(transform.position, player.position) > marketRadius) return;

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
