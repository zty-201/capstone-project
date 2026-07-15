using UnityEngine;

public class PartCollectionSystem : MonoBehaviour
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private int totalParts = 3;
    [SerializeField] private AssemblyPoint assemblyPoint;

    private int collectedCount;

    // Subscribed in Awake/OnDestroy, not OnEnable/OnDisable: this object lives inside a
    // container MinigameActivator disables right after this path's mission completes, so an
    // OnEnable/OnDisable subscription would already be gone by the time a stage fail — which
    // can only happen after the mission is complete — needs to reach it.
    private void Awake() => EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    private void OnEnable()
    {
        collectedCount = 0;
        assemblyPoint.gameObject.SetActive(false);
    }

    public void OnPartCollected()
    {
        collectedCount++;
        if (collectedCount >= totalParts)
            assemblyPoint.gameObject.SetActive(true);
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        collectedCount = 0;
        foreach (var part in GetComponentsInChildren<MachinePart>(true))
            part.ResetPart();
        assemblyPoint.ResetPoint();
    }
}
