using UnityEngine;

// This attribute allows you to create instances of this data directly from the Unity Editor menu
[CreateAssetMenu(fileName = "NewMission", menuName = "Kaizen Systems/Mission Data")]
public class MissionData : ScriptableObject
{
    [Header("Mission Overview")]
    public int missionID; // e.g., "M1_ParchedCrops"
    public string missionName;
    public bool isAdvancedMission;

    [Header("Problem Identification (The Issue)")]
    [TextArea(2, 4)]
    public string villagerComplaint; // What the NPC says initially
    [TextArea(2, 4)]
    public string actualRootCause; // The hidden Gemba truth

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
    public string trivialReflectionText; // Text shown during "Check" if trivial is chosen
    [TextArea(2, 4)]
    public string optimalReflectionText; // Text shown during "Check" if optimal is chosen
}