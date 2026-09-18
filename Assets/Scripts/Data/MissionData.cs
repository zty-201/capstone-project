using UnityEngine;

[CreateAssetMenu(fileName = "NewMission", menuName = "Kaizen Systems/Mission Data")]
public class MissionData : ScriptableObject
{
    [Header("Mission Overview")]
    public int missionID;
    public string missionName;
    public bool isAdvancedMission;

    [Header("Problem Identification (The Issue)")]
    [TextArea(2, 4)]
    public string[] villagerComplaint;
    [TextArea(2, 4)]
    public string actualRootCause;

    [System.Serializable]
    public class WhyStage
    {
        [TextArea(1, 2)]
        public string question;
        public string correctAnswer;
        public string[] distractors;
        [TextArea(1, 2)]
        public string hint;
    }

    [Header("5 Whys Investigation — must reach 5/5 for the optimal path")]
    public WhyStage[] fiveWhys = new WhyStage[5];

    [Header("Advanced Mission Minigame Hint")]
    // Shown only mid-minigame when StageManager.IsMissionUnderReview(missionID) is true — same
    // review-only-hint convention as WhyStage.hint, but for a single mission-wide minigame hint
    // rather than a per-Why-stage one, since an advanced mission's action-phase minigame (e.g.
    // FarmRoutineSystem) isn't structured as discrete Why stages the way the quiz is. Missions
    // that don't need it (e.g. M5_BrokenBridge, which hints via bonus test attempts instead)
    // just leave this blank.
    [TextArea(1, 3)]
    public string minigameHint;

    [Header("Action Phase (Do)")]
    [TextArea(2, 4)]
    public string trivialReflectionText;
    [TextArea(2, 4)]
    public string optimalReflectionText;

    [Header("Mission Directory HUD")]
    // Shown before the player has picked a solution (i.e. before OnSolutionSelected fires),
    // and again the instant a trivial completion gets reopened for review — a redo starts
    // back at square one, so the tracker line resets to this rather than resuming mid-path.
    [TextArea(1, 2)]
    public string introObjective;
    // Ordered sub-stage lines for each path, indexed by EventBus.OnObjectiveProgress's
    // stageIndex. A line containing "{0}"/"{1}" is run through string.Format with the
    // event's count/total (e.g. parts collected so far / parts needed) — plain lines
    // ignore those args.
    [TextArea(1, 2)]
    public string[] trivialObjectives;
    [TextArea(1, 2)]
    public string[] optimalObjectives;
}
