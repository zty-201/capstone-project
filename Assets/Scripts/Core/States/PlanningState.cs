using UnityEngine;
using static MissionData;

public class PlanningState : MonoBehaviour
{
    public static PlanningState Instance { get; private set; }
    private CanvasGroup canvasGroup;
    private MissionData currentMission;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(MissionData mission)
    {
        currentMission = mission;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // Wired to button OnClick in Inspector
    public void SelectTrivial() => SelectSolution(SolutionType.Trivial);
    public void SelectOptimal() => SelectSolution(SolutionType.Optimal);

    private void SelectSolution(SolutionType choice)
    {
        EventBus.OnSolutionSelected?.Invoke(currentMission.missionID, choice);
        Hide();
    }
}