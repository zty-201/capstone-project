using UnityEngine;

public class AssemblyPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private GameObject machineVisual;
    [SerializeField] private PlacementPoint placementPoint;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        if (machineVisual != null) machineVisual.SetActive(true);
        gameObject.SetActive(false);
        if (placementPoint != null) placementPoint.gameObject.SetActive(true);
        EventBus.RaiseObjectiveProgress(missionID, SolutionType.Optimal, 2, 0, 0);
    }

    public void ResetPoint()
    {
        gameObject.SetActive(true);
        if (machineVisual != null) machineVisual.SetActive(false);
        placementPoint?.ResetPoint();
    }
}
