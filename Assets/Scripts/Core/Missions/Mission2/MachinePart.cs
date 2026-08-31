using UnityEngine;

public class MachinePart : MonoBehaviour, IInteractable
{
    [SerializeField] private PartCollectionSystem collectionSystem;
    [SerializeField] private ItemData machinePartItem;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        // Same gate as TrashPiece/BrickPickup: only counts as collected if it actually fit,
        // so a full inventory leaves it on the ground rather than silently losing it.
        if (!InventorySystem.Instance.TryAddItem(machinePartItem, 1)) return;

        collectionSystem.OnPartCollected();
        gameObject.SetActive(false);
    }

    public void ResetPart() => gameObject.SetActive(true);
}
