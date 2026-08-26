using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Data")]
    [SerializeField] private StageRegistry stageRegistry;

    [Header("Coin Requirements")]
    [SerializeField] private ItemData goldCoinItem;
    [SerializeField] private int coinsRequiredToSubmit = 2;

    private int currentStageIndex;
    private int currentDay;

    // missionID -> outcome of its most recent completion (true = optimal).
    private readonly Dictionary<int, bool> missionOutcomes = new Dictionary<int, bool>();
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
    }

    public bool AllMissionsCompleteForCurrentStage()
    {
        if (AllStagesComplete) return false;
        foreach (int id in CurrentStage.missionIDs)
            if (!missionOutcomes.ContainsKey(id)) return false;
        return true;
    }

    public bool AllMissionsOptimalForCurrentStage()
    {
        if (AllStagesComplete) return false;
        foreach (int id in CurrentStage.missionIDs)
            if (!missionOutcomes.TryGetValue(id, out bool wasOptimal) || !wasOptimal) return false;
        return true;
    }

    public bool HasEnoughCoins() =>
        InventorySystem.Instance.CountItem(goldCoinItem) >= coinsRequiredToSubmit;

    public bool IsMissionUnderReview(int missionID) =>
        missionOutcomes.TryGetValue(missionID, out bool wasOptimal) && !wasOptimal;

    public void SubmitStage()
    {
        var needsReview = new List<int>();
        foreach (int id in CurrentStage.missionIDs)
            if (!missionOutcomes[id]) needsReview.Add(id);

        if (needsReview.Count == 0)
        {
            InventorySystem.Instance.TryRemoveItem(goldCoinItem, coinsRequiredToSubmit);

            currentDay++;
            EventBus.RaiseDayCompleted(currentDay);
            missionOutcomes.Clear();
            excludedDistractors.Clear();

            if (currentStageIndex + 1 < stageRegistry.stages.Length)
                currentStageIndex++;
            else
                AllStagesComplete = true;
        }
        else
        {
            EventBus.RaiseMissionsNeedReview(needsReview.ToArray());
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
