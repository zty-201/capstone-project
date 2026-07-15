using UnityEngine;

public class TownSatisfactionSystem : MonoBehaviour
{
    public static TownSatisfactionSystem Instance { get; private set; }

    [Header("Mission Data")]
    [SerializeField] private MissionRegistry missionRegistry;

    [Header("Settings")]
    [SerializeField] private int startingSatisfaction = 50;
    [SerializeField] private int maxSatisfaction = 100;

    public int CurrentSatisfaction { get; private set; }
    public int MaxSatisfaction => maxSatisfaction;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (missionRegistry == null) Debug.LogError($"[{name}] missionRegistry is not assigned!", this);

        CurrentSatisfaction = startingSatisfaction;
    }

    private void Start() => EventBus.RaiseSatisfactionChanged(CurrentSatisfaction);

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        MissionData data = missionRegistry.GetByID(missionID);
        if (data == null)
        {
            Debug.LogError($"[TownSatisfactionSystem] No MissionData found for ID {missionID}");
            return;
        }

        ApplyDelta(wasOptimal ? data.optimalSatisfactionReward : data.trivialSatisfactionReward);
    }

    /// <returns>The actual change applied after clamping (may differ from delta at the 0/max edges).</returns>
    public int ApplyDelta(int delta)
    {
        int before = CurrentSatisfaction;
        CurrentSatisfaction = Mathf.Clamp(CurrentSatisfaction + delta, 0, maxSatisfaction);
        EventBus.RaiseSatisfactionChanged(CurrentSatisfaction);
        return CurrentSatisfaction - before;
    }

    // Called directly by StageManager after OnDayCompleted has already been raised and read
    // by display subscribers — never wired as an event subscriber itself, so the reset can't
    // race the score it's resetting.
    public void ResetToBaseline()
    {
        CurrentSatisfaction = startingSatisfaction;
        EventBus.RaiseSatisfactionChanged(CurrentSatisfaction);
    }

    // Called directly by StageManager the first time a mission is flagged for review, to undo
    // the trivial reward it earned until it's redone optimally.
    public void RetractTrivialReward(int missionID)
    {
        MissionData data = missionRegistry.GetByID(missionID);
        if (data == null)
        {
            Debug.LogError($"[TownSatisfactionSystem] No MissionData found for ID {missionID}");
            return;
        }

        ApplyDelta(-data.trivialSatisfactionReward);
    }
}
