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

    [Header("5W Model Data (Investigation)")]
    public string who;
    public string what;
    public string where;
    public string when;
    public string why;

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
