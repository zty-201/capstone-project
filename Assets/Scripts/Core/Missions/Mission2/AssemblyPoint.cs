using UnityEngine;

public class AssemblyPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private GameObject machineVisual;

    public void Interact()
    {
        if (machineVisual != null) machineVisual.SetActive(true);
        gameObject.SetActive(false);
        EventBus.RaiseMissionCompleted(missionID, true);
    }
}
