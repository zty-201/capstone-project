using UnityEngine;

public class PlacementPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private GameObject placedMachineVisual;

    public void Interact()
    {
        if (placedMachineVisual != null) placedMachineVisual.SetActive(true);
        gameObject.SetActive(false);
        EventBus.RaiseMissionCompleted(missionID, true);
    }

    public void ResetPoint()
    {
        gameObject.SetActive(false);
        if (placedMachineVisual != null) placedMachineVisual.SetActive(false);
    }
}
