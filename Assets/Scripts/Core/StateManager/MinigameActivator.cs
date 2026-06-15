using UnityEngine;
using static MissionData;

public class MinigameActivator : MonoBehaviour
{
    [Header("Mission Identity")]
    public int missionID;
    public SolutionType solutionType;

    [Header("Target")]
    public GameObject container;

    public enum MinigameStateType { Puzzle, PatchWell }
    [Header("State To Enter")]
    public MinigameStateType stateToEnter;

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

        IState target = stateToEnter == MinigameStateType.Puzzle
            ? (IState)GameManager.Instance.PuzzleState
            : GameManager.Instance.PatchWellState;
        GameManager.Instance.StateManager.ChangeState(target);
    }

    private void HandleMissionCompleted(int completedMissionID, bool wasOptimal)
    {
        if (completedMissionID != missionID) return;

        // Only deactivate the path that was actually played
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