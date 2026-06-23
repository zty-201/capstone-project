using UnityEngine;

public class RiverManager : MonoBehaviour
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private GameObject blockageVisual;
    [SerializeField] private GameObject animatedRiverTilemap;

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    private void HandleMissionCompleted(int id, bool wasOptimal)
    {
        if (id != missionID) return;
        if (blockageVisual != null) blockageVisual.SetActive(false);
        if (animatedRiverTilemap != null) animatedRiverTilemap.SetActive(true);
    }
}
