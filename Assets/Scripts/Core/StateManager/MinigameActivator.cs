using UnityEngine;
using static MissionData;

public class MinigameActivator : MonoBehaviour
{
    [Header("Mission Identity")]
    public int missionID;
    public SolutionType solutionType;

    [Header("Target")]
    public GameObject container;

    private void OnEnable()
    {
        EventBus.OnSolutionSelected += HandleSolutionSelected;
        EventBus.OnMissionCompleted += HandleMissionCompleted;
    }

    private void OnDisable()
    {
        EventBus.OnSolutionSelected -= HandleSolutionSelected;
        EventBus.OnMissionCompleted -= HandleMissionCompleted;
    }

    private void HandleSolutionSelected(int selectedMissionID, SolutionType type)
    {
        if (selectedMissionID != missionID || type != solutionType) return;
        container.SetActive(true);
    }

    private void HandleMissionCompleted(int completedMissionID, bool wasOptimal)
    {
        if (completedMissionID != missionID) return;
        container.SetActive(false);
    }
}