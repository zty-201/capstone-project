using UnityEngine;

public class AssemblyPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject machineVisual;
    [SerializeField] private PlacementPoint placementPoint;

    public void Interact()
    {
        if (machineVisual != null) machineVisual.SetActive(true);
        gameObject.SetActive(false);
        if (placementPoint != null) placementPoint.gameObject.SetActive(true);
    }

    public void ResetPoint()
    {
        gameObject.SetActive(true);
        if (machineVisual != null) machineVisual.SetActive(false);
        placementPoint?.ResetPoint();
    }
}
