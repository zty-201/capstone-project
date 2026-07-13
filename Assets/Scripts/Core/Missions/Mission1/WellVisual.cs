using System.Collections;
using UnityEngine;

public class WellVisual : MonoBehaviour, IInteractable
{
    [Header("Mission Identity")]
    public int missionID = 1;

    public void Interact()
    {
        StartCoroutine(HandlePatchVictory());
    }

    private IEnumerator HandlePatchVictory()
    {
        Debug.Log("<color=cyan>[WellVisual]</color> Well patched!");
        yield return new WaitForSeconds(1f);

        EventBus.RaiseMissionCompleted(missionID, false);
    }
}
