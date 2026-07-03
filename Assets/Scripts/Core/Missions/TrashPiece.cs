using UnityEngine;

public class TrashPiece : MonoBehaviour, IInteractable
{
    private TrashSpawner spawner;
    private Transform spawnPoint;
    private int accumulatedLoss;

    public void Init(TrashSpawner owningSpawner, Transform ownSpawnPoint)
    {
        spawner = owningSpawner;
        spawnPoint = ownSpawnPoint;
    }

    public void TrackLoss(int amount) => accumulatedLoss += amount;

    public void Interact()
    {
        if (accumulatedLoss > 0) TownSatisfactionSystem.Instance.ApplyDelta(accumulatedLoss);
        spawner.RemoveTrash(spawnPoint);
        Destroy(gameObject);
    }
}
