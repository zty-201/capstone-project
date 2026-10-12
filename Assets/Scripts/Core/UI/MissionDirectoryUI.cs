using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Mission objective tracker, in two views over the same data:
// - Collapsed (top-left HUD): only the `collapsedLines.Length` most recently active unresolved
//   missions, objective text only. Tapping it (a Button on this GameObject → Open) expands.
// - Expanded (centered panel): every mission, name + objective/status — this replaced the old
//   Mission Board. Tapping outside the window (a full-screen backdrop Button → Close) collapses.
// Purely reactive — it never holds a reference into a mission script; it only listens to
// EventBus, the sole coupling layer between systems.
// Line content itself lives in MissionData (introObjective/trivialObjectives/optimalObjectives),
// matching how ReflectionPopupUI already sources its display text from data rather than from
// event payloads. OnObjectiveProgress carries its own SolutionType rather than this UI inferring
// the active path from a separate OnSolutionSelected subscription — that would race against
// MinigameActivator raising this same-frame event, since subscriber order between two different
// EventBus events isn't guaranteed.
public class MissionDirectoryUI : MonoBehaviour
{
    public static MissionDirectoryUI Instance { get; private set; }

    [System.Serializable]
    public class DirectoryEntry
    {
        public MissionData missionData;
        // This mission's line in the expanded panel.
        public TextMeshProUGUI lineText;

        [System.NonSerialized] public string objective;
        [System.NonSerialized] public bool resolved;
    }

    [Header("Expanded Panel — one entry per mission")]
    [SerializeField] private DirectoryEntry[] entries;
    // Root of the expanded overlay: a sibling of this HUD object (not a child — a click inside
    // the window would otherwise bubble up to this object's own open Button), holding the
    // full-screen backdrop Button and the centered window.
    [SerializeField] private CanvasGroup expandedPanel;

    [Header("Collapsed HUD")]
    // Its length is how many missions the collapsed HUD shows.
    [SerializeField] private TextMeshProUGUI[] collapsedLines;

    [SerializeField] private float resolvedLineAlpha = 0.4f;

    // Most recently active first — decides which missions the collapsed HUD shows.
    private readonly List<DirectoryEntry> recency = new List<DirectoryEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (expandedPanel == null) Debug.LogError($"[{name}] expandedPanel is not assigned!", this);

        HideExpanded();
    }

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
        {
            entry.objective = entry.missionData.introObjective;
            recency.Add(entry);
        }
        Refresh();
    }

    // Wired to the collapsed HUD's Button OnClick in the Inspector. Only opens from Exploration:
    // the HUD stays clickable during minigames, and closing returns to Exploration, which would
    // strand the player outside a minigame they have no way back into.
    public void Open()
    {
        if (GameManager.Instance.StateManager.CurrentStateType != GameStateType.Exploration) return;

        expandedPanel.alpha = 1f;
        expandedPanel.interactable = true;
        expandedPanel.blocksRaycasts = true;
        GameManager.Instance.StateManager.ChangeState(GameStateType.MissionDirectory);
    }

    // Wired to the backdrop Button OnClick in the Inspector (tap outside the window).
    public void Close() => GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);

    // Called by MissionDirectoryState.Exit.
    public void HideExpanded()
    {
        expandedPanel.alpha = 0f;
        expandedPanel.interactable = false;
        expandedPanel.blocksRaycasts = false;
    }

    private void HandleObjectiveProgress(int missionID, SolutionType path, int stageIndex, int count, int total)
    {
        var entry = FindEntry(missionID);
        if (entry == null) return;

        string[] objectives = path == SolutionType.Optimal
            ? entry.missionData.optimalObjectives
            : entry.missionData.trivialObjectives;

        if (objectives == null || stageIndex < 0 || stageIndex >= objectives.Length) return;

        entry.objective = string.Format(objectives[stageIndex], count, total);
        MarkActive(entry);
    }

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        var entry = FindEntry(missionID);
        if (entry == null) return;

        // Optimal: fully resolved — drops off the collapsed HUD, greyed in the expanded panel.
        // Trivial: still outstanding (the rushed fix will break down for a redo).
        entry.resolved = wasOptimal;
        entry.objective = wasOptimal ? "Resolved" : "Needs Review";
        MarkActive(entry);
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        foreach (int id in missionIDs)
        {
            var entry = FindEntry(id);
            if (entry == null) continue;
            // A redo starts back at square one (re-interact to re-open dialogue), so the
            // tracker resets to the intro line rather than resuming mid-path.
            entry.objective = entry.missionData.introObjective;
            entry.resolved = false;
            MarkActive(entry);
        }
    }

    private void MarkActive(DirectoryEntry entry)
    {
        recency.Remove(entry);
        recency.Insert(0, entry);
        Refresh();
    }

    private void Refresh()
    {
        foreach (var entry in entries)
        {
            entry.lineText.text = $"<b>{entry.missionData.missionName}</b>\n{entry.objective}";
            entry.lineText.alpha = entry.resolved ? resolvedLineAlpha : 1f;
        }

        int shown = 0;
        foreach (var entry in recency)
        {
            if (shown == collapsedLines.Length) break;
            if (entry.resolved) continue;
            collapsedLines[shown].text = entry.objective;
            collapsedLines[shown].gameObject.SetActive(true);
            shown++;
        }
        for (int i = shown; i < collapsedLines.Length; i++)
            collapsedLines[i].gameObject.SetActive(false);
    }

    private DirectoryEntry FindEntry(int missionID)
    {
        foreach (var entry in entries)
            if (entry.missionData.missionID == missionID) return entry;
        return null;
    }
}
