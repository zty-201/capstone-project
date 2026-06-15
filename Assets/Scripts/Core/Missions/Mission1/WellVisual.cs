using System.Collections;
using UnityEngine;
using static MissionData;

public class WellVisual : MonoBehaviour
{
    [Header("Mission Identity")]
    public int missionID = 1;

    private BoxCollider2D col;
    private SolutionType solutionTypeUsed;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        EventBus.OnSolutionSelected += HandleSolutionSelected;
        EventBus.OnWellClicked += HandleWellClicked;
    }

    private void OnDisable()
    {
        EventBus.OnSolutionSelected -= HandleSolutionSelected;
        EventBus.OnWellClicked -= HandleWellClicked;
    }

    private void HandleSolutionSelected(int selectedMissionID, SolutionType type)
    {
        if (selectedMissionID != missionID || type != SolutionType.Trivial) return;

        solutionTypeUsed = type;
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.PatchWellState);
    }

    private void HandleWellClicked(Vector3 worldPos)
    {
        if (GameManager.Instance.StateManager.CurrentState != GameManager.Instance.PatchWellState) return;

        if (col.OverlapPoint(worldPos))
        {
            StartCoroutine(HandlePatchVictory());
        }
    }

    private IEnumerator HandlePatchVictory()
    {
        Debug.Log("<color=cyan>[WellVisual]</color> Well patched!");
        yield return new WaitForSeconds(1f);

        EventBus.RaiseMissionCompleted(missionID, solutionTypeUsed == SolutionType.Optimal);
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ExploreState);
    }
}