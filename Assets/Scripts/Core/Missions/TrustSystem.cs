using System.Collections.Generic;
using UnityEngine;

// Visual-feedback-only relationship meter per mission: rises on an optimal solve, drops on a
// trivial one. Deliberately decoupled from StageManager's mission-outcome bookkeeping — trust is
// a standing signal that persists across stages/days, it doesn't gate reattempts (that's still
// entirely handled by StageManager -> OnMissionsNeedReview).
public class TrustSystem : MonoBehaviour
{
    public static TrustSystem Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int startingTrust = 2;
    [SerializeField] private int maxTrust = 5;
    [SerializeField] private int trustGainOnOptimal = 1;
    [SerializeField] private int trustLossOnTrivial = 1;

    private readonly Dictionary<int, int> trustByMission = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    public int GetTrust(int missionID) =>
        trustByMission.TryGetValue(missionID, out int value) ? value : startingTrust;

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        int current = GetTrust(missionID);
        current = Mathf.Clamp(current + (wasOptimal ? trustGainOnOptimal : -trustLossOnTrivial), 0, maxTrust);
        trustByMission[missionID] = current;
        EventBus.RaiseTrustChanged(missionID, current);
    }
}
