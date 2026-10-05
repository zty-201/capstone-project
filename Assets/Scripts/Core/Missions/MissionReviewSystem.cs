using System.Collections.Generic;
using UnityEngine;

// The game's "Check" step, expressed in the world rather than at a desk: a mission resolved
// trivially is a rushed fix, and rushed fixes don't last. The next time the player completes any
// *other* mission, every rushed fix still standing breaks down and its mission reopens (raised as
// OnMissionsNeedReview, which every mission's own reset logic already listens for). If the rushed
// mission is the only one left unsolved, there is no "other" mission to wait for, so it breaks
// down right away — otherwise the player could never finish.
//
// Breakdowns are queued at completion and only raised once the reflection popup is dismissed
// (OnReflectionDismissed): raising them inside the same OnMissionCompleted dispatch would race the
// completing mission's own teardown (MinigameActivator, interactables disabling themselves), and
// would fight ReflectionPopupUI for the current game state.
//
// Also owns the per-mission redo scaffolding the 5 Whys quiz and minigames read: whether a mission
// is under review (shows hints) and which wrong answers to exclude on a retry.
public class MissionReviewSystem : MonoBehaviour
{
    public static MissionReviewSystem Instance { get; private set; }

    [SerializeField] private MissionRegistry missionRegistry;

    // missionID -> outcome of its most recent completion (true = optimal).
    private readonly Dictionary<int, bool> missionOutcomes = new Dictionary<int, bool>();
    private readonly Dictionary<(int missionID, int whyIndex), HashSet<string>> excludedDistractors
        = new Dictionary<(int, int), HashSet<string>>();

    // Rushed fixes still standing, waiting for the next other mission to be completed.
    private readonly HashSet<int> standingRushedFixes = new HashSet<int>();
    // Rushed fixes that will break down once the current reflection popup is dismissed.
    private readonly List<int> pendingBreakdowns = new List<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (missionRegistry == null) Debug.LogError($"[{name}] missionRegistry is not assigned!", this);
    }

    private void OnEnable()
    {
        EventBus.OnMissionCompleted += HandleMissionCompleted;
        EventBus.OnReflectionDismissed += HandleReflectionDismissed;
    }

    private void OnDisable()
    {
        EventBus.OnMissionCompleted -= HandleMissionCompleted;
        EventBus.OnReflectionDismissed -= HandleReflectionDismissed;
    }

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        missionOutcomes[missionID] = wasOptimal;

        // Completing this mission is what makes every *earlier* rushed fix give way.
        standingRushedFixes.Remove(missionID);
        pendingBreakdowns.AddRange(standingRushedFixes);
        standingRushedFixes.Clear();

        if (wasOptimal) return;

        if (AllOtherMissionsOptimal(missionID)) pendingBreakdowns.Add(missionID);
        else standingRushedFixes.Add(missionID);
    }

    private void HandleReflectionDismissed()
    {
        if (pendingBreakdowns.Count == 0) return;

        int[] brokenDown = pendingBreakdowns.ToArray();
        pendingBreakdowns.Clear();
        EventBus.RaiseMissionsNeedReview(brokenDown);
    }

    private bool AllOtherMissionsOptimal(int missionID)
    {
        foreach (MissionData mission in missionRegistry.missions)
        {
            if (mission.missionID == missionID) continue;
            if (!missionOutcomes.TryGetValue(mission.missionID, out bool wasOptimal) || !wasOptimal) return false;
        }
        return true;
    }

    public bool IsMissionUnderReview(int missionID) =>
        missionOutcomes.TryGetValue(missionID, out bool wasOptimal) && !wasOptimal;

    public HashSet<string> GetExcludedDistractors(int missionID, int whyIndex)
    {
        excludedDistractors.TryGetValue((missionID, whyIndex), out var set);
        return set;
    }

    public void RecordWrongAnswer(int missionID, int whyIndex, string wrongPick)
    {
        var key = (missionID, whyIndex);
        if (!excludedDistractors.TryGetValue(key, out var set))
        {
            set = new HashSet<string>();
            excludedDistractors[key] = set;
        }
        set.Add(wrongPick);
    }
}
