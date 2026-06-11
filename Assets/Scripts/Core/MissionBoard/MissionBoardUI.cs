using UnityEngine;

public class MissionBoardUI : MonoBehaviour
{
    public static MissionBoardUI Instance { get; private set; }

    [Header("Mission Entries")]
    public MissionEntryUI[] missionEntries; // One per mission, assigned in Inspector

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        // Find the matching entry and grey it out
        foreach (var entry in missionEntries)
        {
            if (entry.missionID == missionID)
            {
                entry.MarkCompleted(wasOptimal);
                return;
            }
        }
    }
}