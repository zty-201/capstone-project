using UnityEngine;

public class MissionBoardUI : MonoBehaviour
{
    public static MissionBoardUI Instance { get; private set; }

    [Header("Mission Entries")]
    public MissionEntryUI[] missionEntries; // One per mission, assigned in Inspector

    private CanvasGroup canvasGroup;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        HidePanel();
    }

    private void HidePanel()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ShowPanel()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    public void Show() => ShowPanel();
    public void Hide() => HidePanel();

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        // Find the matching entry and grey it out
        foreach (var entry in missionEntries)
        {
            if (entry.missionData.missionID == missionID)
            {
                entry.MarkCompleted(wasOptimal);
                return;
            }
        }
    }
}