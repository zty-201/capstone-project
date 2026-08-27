using UnityEngine;

public class TrashPiece : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData trashItem;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    private TrashSpawner spawner;
    private Transform spawnPoint;

    public void Init(TrashSpawner owningSpawner, Transform ownSpawnPoint)
    {
        spawner = owningSpawner;
        spawnPoint = ownSpawnPoint;
    }

    public void Interact()
    {
        // Trash doesn't stack, so this fails once the inventory is full of other trash/items —
        // leave the piece on the ground rather than losing it.
        if (!InventorySystem.Instance.TryAddItem(trashItem, 1)) return;

        spawner.RemoveTrash(spawnPoint);
        Destroy(gameObject);
    }
}
