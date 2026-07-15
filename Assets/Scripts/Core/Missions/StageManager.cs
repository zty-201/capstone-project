using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Data")]
    [SerializeField] private StageRegistry stageRegistry;

    private int currentStageIndex;
    private int currentDay;

    // missionID -> outcome of its most recent completion (true = optimal).
    private readonly Dictionary<int, bool> missionOutcomes = new Dictionary<int, bool>();
    // Missions whose latest trivial completion hasn't had its reward retracted at a submission
    // yet. Re-armed on every trivial completion (not just the first) — otherwise a redo attempt
    // that's still wrong would re-earn the trivial reward via TownSatisfactionSystem's own
    // OnMissionCompleted handler with nothing here to cancel it back out, letting the score
    // creep up on repeated failed redos instead of staying capped at "the optimal missions only."
    private readonly HashSet<int> pendingRetraction = new HashSet<int>();
    private readonly Dictionary<(int missionID, int whyIndex), HashSet<string>> excludedDistractors
        = new Dictionary<(int, int), HashSet<string>>();

    public bool AllStagesComplete { get; private set; }

    private StageData CurrentStage => stageRegistry.GetByIndex(currentStageIndex);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (stageRegistry == null) Debug.LogError($"[{name}] stageRegistry is not assigned!", this);

        currentDay = 1;
    }

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        if (AllStagesComplete) return;
        if (System.Array.IndexOf(CurrentStage.missionIDs, missionID) < 0) return;

        missionOutcomes[missionID] = wasOptimal;
        if (wasOptimal) pendingRetraction.Remove(missionID);
        else pendingRetraction.Add(missionID);
    }

    public bool AllMissionsCompleteForCurrentStage()
    {
        if (AllStagesComplete) return false;
        foreach (int id in CurrentStage.missionIDs)
            if (!missionOutcomes.ContainsKey(id)) return false;
        return true;
    }

    public bool IsMissionUnderReview(int missionID) =>
        missionOutcomes.TryGetValue(missionID, out bool wasOptimal) && !wasOptimal;

    public void SubmitStage()
    {
        var needsReview = new List<int>();
        foreach (int id in CurrentStage.missionIDs)
            if (!missionOutcomes[id]) needsReview.Add(id);

        if (needsReview.Count == 0)
        {
            currentDay++;
            EventBus.RaiseDayCompleted(currentDay);
            TownSatisfactionSystem.Instance.ResetToBaseline();
            missionOutcomes.Clear();
            pendingRetraction.Clear();
            excludedDistractors.Clear();

            if (currentStageIndex + 1 < stageRegistry.stages.Length)
                currentStageIndex++;
            else
                AllStagesComplete = true;
        }
        else
        {
            // Raise first so the summary panel reads the score the player actually earned this
            // attempt (e.g. 85 for one optimal + one trivial mission), then retract afterward so
            // the trivial mission's reward is no longer banked for the redo that follows.
            EventBus.RaiseMissionsNeedReview(needsReview.ToArray());

            foreach (int id in needsReview)
                if (pendingRetraction.Remove(id))
                    TownSatisfactionSystem.Instance.RetractTrivialReward(id);
        }
    }

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
