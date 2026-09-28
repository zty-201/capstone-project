using System.Collections.Generic;
using UnityEngine;

// The permanent, post-mission epilogue for Mission 4 — entirely separate from
// Container_Trivial_M4/Container_Optimal_M4 (the temporary Do-phase minigame containers, which
// MinigameActivator disables once the mission resolves and which this system never touches).
// This lives in the Mission4 world group on its own persistent set of MarketStalls and starts
// operating only once OnMissionCompleted fires for missionID 4 — same trigger RiverManager uses
// for its own permanent post-completion visual swap, except here the "swap" is an ongoing
// simulation rather than a one-time flip.
//
// Two permanent modes, locked in once by the mission's outcome and never re-evaluated:
// - Trivial ("Unmanaged"): an attendant restocks a random stall on a random timer, completely
//   decoupled from that stall's actual stock level — dramatizing "nobody's watching the
//   threshold" literally, not just in the reflection text.
// - Optimal ("Kanban"): the moment any stall's live stock crosses its own tuned threshold — the
//   exact value the player dragged into place in KanbanBuilderSystem (GetThresholdRatio), not a
//   fresh hardcoded ideal — an attendant is dispatched immediately. The walk itself stands in for
//   delivery lead time; there's no separate abstract timer for it, since a real pathfound walk
//   already takes real time proportional to distance.
//
// Attendant count is however many MarketAttendantNPCs are dragged into `attendants[]` — nothing
// here assumes a fixed number, so the designer can freely add or remove attendants to see how
// busy or quiet the marketplace should feel.
public class MarketAmbientSystem : MonoBehaviour
{
    [SerializeField] private int missionID = 4;
    [SerializeField] private MarketStall[] stalls;
    [SerializeField] private MarketAttendantNPC[] attendants;

    [Header("Trivial (\"Unmanaged\") mode")]
    [SerializeField] private float minRestockInterval = 8f;
    [SerializeField] private float maxRestockInterval = 20f;

    private bool modeActive;
    private bool isOptimal;
    private float restockTimer;
    private float nextRestockInterval;
    private float[] thresholdRatio;

    private readonly Queue<int> pendingJobs = new Queue<int>();
    private readonly HashSet<int> queuedStalls = new HashSet<int>();

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    private void HandleMissionCompleted(int id, bool wasOptimal)
    {
        if (id != missionID) return;

        isOptimal = wasOptimal;
        modeActive = true;
        restockTimer = 0f;
        nextRestockInterval = Random.Range(minRestockInterval, maxRestockInterval);
        pendingJobs.Clear();
        queuedStalls.Clear();

        thresholdRatio = new float[stalls.Length];
        for (int i = 0; i < stalls.Length; i++)
        {
            stalls[i].ResetStall();
            // Optimal epilogue reuses the player's own tuned threshold; the trivial epilogue
            // never reads it at all (TickUnmanagedMode ignores thresholdRatio entirely), so 0
            // here is just an unused placeholder, not a design choice.
            thresholdRatio[i] = isOptimal && KanbanBuilderSystem.Instance != null
                ? KanbanBuilderSystem.Instance.GetThresholdRatio(i)
                : 0f;
        }
    }

    private void Update()
    {
        if (!modeActive) return;
        if (GameManager.Instance.StateManager.CurrentStateType != GameStateType.Exploration) return;

        for (int i = 0; i < stalls.Length; i++)
            stalls[i].TickDeplete(Time.deltaTime);

        if (isOptimal) TickKanbanMode();
        else TickUnmanagedMode(Time.deltaTime);

        DispatchAvailableAttendants();
    }

    private void TickUnmanagedMode(float dt)
    {
        restockTimer += dt;
        if (restockTimer < nextRestockInterval) return;

        restockTimer = 0f;
        nextRestockInterval = Random.Range(minRestockInterval, maxRestockInterval);
        // Picks a stall at random regardless of how full it actually is — the point is that
        // nobody's watching the threshold, so restocking here has no relationship to real need.
        QueueJob(Random.Range(0, stalls.Length));
    }

    private void TickKanbanMode()
    {
        for (int i = 0; i < stalls.Length; i++)
            if (stalls[i].StockRatio <= thresholdRatio[i])
                QueueJob(i);
    }

    private void QueueJob(int stallIndex)
    {
        if (queuedStalls.Contains(stallIndex)) return;
        queuedStalls.Add(stallIndex);
        pendingJobs.Enqueue(stallIndex);
    }

    private void DispatchAvailableAttendants()
    {
        foreach (var attendant in attendants)
        {
            if (pendingJobs.Count == 0) break;
            if (attendant == null || !attendant.IsAvailable) continue;

            int stallIndex = pendingJobs.Dequeue();
            attendant.DispatchTo(stalls[stallIndex].transform.position, () =>
            {
                stalls[stallIndex].ResetStall();
                queuedStalls.Remove(stallIndex);
            });
        }
    }
}
