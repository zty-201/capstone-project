using UnityEngine;

public class RiverManager : MonoBehaviour
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private GameObject blockageVisual;
    [SerializeField] private GameObject animatedRiverTilemap;
    [SerializeField] private GameObject[] wastePieces;

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    private void HandleMissionCompleted(int id, bool wasOptimal)
    {
        if (id != missionID) return;
        if (blockageVisual != null) blockageVisual.SetActive(false);
        if (animatedRiverTilemap != null) animatedRiverTilemap.SetActive(true);

        foreach (var piece in wastePieces)
            if (piece != null) piece.SetActive(false);
    }
}
