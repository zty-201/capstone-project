using UnityEngine;
using TMPro;

// Compressed one-line-per-active-mission objective tracker, top-left HUD (see
// Docs/TODO.md). Purely reactive — it never holds a reference into a mission script; it only
// listens to EventBus, the sole coupling layer between systems.
// Line content itself lives in MissionData (introObjective/trivialObjectives/optimalObjectives),
// matching how ReflectionPopupUI/MissionEntryUI already source their display text from data
// rather than from event payloads. OnObjectiveProgress carries its own SolutionType rather
// than this UI inferring the active path from a separate OnSolutionSelected subscription —
// that would race against MinigameActivator raising this same-frame event, since subscriber
// order between two different EventBus events isn't guaranteed.
public class MissionDirectoryUI : MonoBehaviour
{
    [System.Serializable]
    public class DirectoryEntry
    {
        public MissionData missionData;
        public TextMeshProUGUI lineText;
    }

    [Header("Entries — one per mission")]
    [SerializeField] private DirectoryEntry[] entries;

    private void OnEnable()
    {
        EventBus.OnObjectiveProgress += HandleObjectiveProgress;
        EventBus.OnMissionCompleted += HandleMissionCompleted;
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDisable()
    {
        EventBus.OnObjectiveProgress -= HandleObjectiveProgress;
        EventBus.OnMissionCompleted -= HandleMissionCompleted;
        EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;
    }

    private void Start()
    {
        foreach (var entry in entries)
            SetLine(entry, entry.missionData.introObjective);
    }

    private void HandleObjectiveProgress(int missionID, SolutionType path, int stageIndex, int count, int total)
    {
        var entry = FindEntry(missionID);
        if (entry == null) return;

        string[] objectives = path == SolutionType.Optimal
            ? entry.missionData.optimalObjectives
            : entry.missionData.trivialObjectives;

        if (objectives == null || stageIndex < 0 || stageIndex >= objectives.Length) return;

        SetLine(entry, string.Format(objectives[stageIndex], count, total));
    }

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        var entry = FindEntry(missionID);
        if (entry == null) return;

        // Optimal: fully resolved, nothing left to track — drop the line entirely.
        // Trivial: matches MissionEntryUI's exact wording, since it's the same outstanding
        // state (still needs a Stage Gate redo) shown by the Mission Board.
        if (wasOptimal) entry.lineText.gameObject.SetActive(false);
        else SetLine(entry, "Needs Review");
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        foreach (int id in missionIDs)
        {
            var entry = FindEntry(id);
            if (entry == null) continue;
            // A redo starts back at square one (re-interact to re-open dialogue), so the
            // tracker resets to the intro line rather than resuming mid-path.
            SetLine(entry, entry.missionData.introObjective);
        }
    }

    private DirectoryEntry FindEntry(int missionID)
    {
        foreach (var entry in entries)
            if (entry.missionData.missionID == missionID) return entry;
        return null;
    }

    private static void SetLine(DirectoryEntry entry, string text)
    {
        entry.lineText.gameObject.SetActive(true);
        entry.lineText.text = text;
    }
}
