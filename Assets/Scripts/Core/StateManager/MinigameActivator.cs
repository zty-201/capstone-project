using UnityEngine;

public class MinigameActivator : MonoBehaviour
{
    [Header("Mission Identity")]
    public int missionID;
    public SolutionType solutionType;

    [Header("Target")]
    public GameObject container;
    public GameStateType targetState;

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
        GameManager.Instance.StateManager.ChangeState(targetState);
    }

    private void HandleMissionCompleted(int completedMissionID, bool wasOptimal)
    {
        if (completedMissionID != missionID) return;

        bool thisPathWasPlayed = (solutionType == SolutionType.Optimal && wasOptimal)
                              || (solutionType == SolutionType.Trivial && !wasOptimal);
        if (!thisPathWasPlayed) return;

        if (container == null)
        {
            Debug.LogError($"[MinigameActivator] container unassigned: mission {missionID} ({solutionType}).");
            return;
        }
        container.SetActive(false);
    }
}
