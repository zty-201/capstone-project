using UnityEngine;

public class RiverManager : MonoBehaviour
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private GameObject blockageVisual;
    [SerializeField] private GameObject animatedRiverTilemap;
    [SerializeField] private GameObject[] wastePieces;

    private void OnEnable()
    {
        EventBus.OnMissionCompleted += HandleMissionCompleted;
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDisable()
    {
        EventBus.OnMissionCompleted -= HandleMissionCompleted;
        EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;
    }

    private void HandleMissionCompleted(int id, bool wasOptimal)
    {
        if (id != missionID) return;
        if (blockageVisual != null) blockageVisual.SetActive(false);
        if (animatedRiverTilemap != null) animatedRiverTilemap.SetActive(true);

        foreach (var piece in wastePieces)
            if (piece != null) piece.SetActive(false);
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        if (blockageVisual != null) blockageVisual.SetActive(true);
        if (animatedRiverTilemap != null) animatedRiverTilemap.SetActive(false);

        foreach (var piece in wastePieces)
            if (piece != null) piece.SetActive(true);
    }
}
