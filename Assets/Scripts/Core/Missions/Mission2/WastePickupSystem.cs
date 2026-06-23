using UnityEngine;

public class WastePickupSystem : MonoBehaviour
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private int wasteCount;

    private int remaining;

    private void OnEnable()
    {
        remaining = wasteCount;
    }

    public void OnWasteRemoved()
    {
        remaining--;
        if (remaining <= 0)
            EventBus.RaiseMissionCompleted(missionID, false);
    }
}
