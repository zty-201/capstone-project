using UnityEngine;

public class WastePickupSystem : MonoBehaviour
{
    [SerializeField] private int missionID = 2;

    private int remaining;

    private void OnEnable()
    {
        remaining = GetComponentsInChildren<WastePiece>().Length;
    }

    public void OnWasteRemoved()
    {
        remaining--;
        if (remaining <= 0)
            EventBus.RaiseMissionCompleted(missionID, false);
    }
}
