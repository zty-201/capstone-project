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

    [Header("Action Phase (Do)")]
    public string trivialSolutionName;
    public string optimalSolutionName;

    [TextArea(2, 4)]
    public string trivialReflectionText;
    [TextArea(2, 4)]
    public string optimalReflectionText;

    [Header("Town Satisfaction Impact")]
    public int trivialSatisfactionReward = 10;
    public int optimalSatisfactionReward = 25;
}
