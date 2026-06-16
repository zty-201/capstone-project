using System.Collections;
using UnityEngine;

public class WellVisual : MonoBehaviour
{
    [Header("Mission Identity")]
    public int missionID = 1;

    private BoxCollider2D col;

    private void Awake() => col = GetComponent<BoxCollider2D>();

    private void OnEnable() => EventBus.OnWellClicked += HandleWellClicked;
    private void OnDisable() => EventBus.OnWellClicked -= HandleWellClicked;

    private void HandleWellClicked(Vector3 worldPos)
    {
        if (GameManager.Instance.StateManager.CurrentStateType != GameStateType.PatchWell) return;
        if (col.OverlapPoint(worldPos)) StartCoroutine(HandlePatchVictory());
    }

    private IEnumerator HandlePatchVictory()
    {
        Debug.Log("<color=cyan>[WellVisual]</color> Well patched!");
        yield return new WaitForSeconds(1f);

        EventBus.RaiseMissionCompleted(missionID, false);
    }
}
