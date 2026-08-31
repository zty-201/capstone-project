using UnityEngine;

public class AssemblyPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private GameObject machineVisual;
    [SerializeField] private PlacementPoint placementPoint;

    [Header("Inventory")]
    [SerializeField] private ItemData machinePartItem;
    [SerializeField] private int partsToConsume = 3;
    [SerializeField] private ItemData winchItem;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        // Trade the 3 collected parts for the assembled Winch — PartCollectionSystem already
        // guaranteed we have partsToConsume of them before activating this object.
        InventorySystem.Instance.TryRemoveItem(machinePartItem, partsToConsume);
        InventorySystem.Instance.TryAddItem(winchItem, 1);

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
