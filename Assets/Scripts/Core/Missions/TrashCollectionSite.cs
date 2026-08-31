using UnityEngine;

// Plain walk-up-and-interact site, same shape as RiverInteractable. Empties every
// Trash item out of the player's inventory in one interaction.
public class TrashCollectionSite : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData trashItem;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        InventorySystem.Instance.RemoveAllOfItem(trashItem);
    }
}
