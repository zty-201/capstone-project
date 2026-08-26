using UnityEngine;

// Plain walk-up-and-interact site, same shape as WellVisual/RiverInteractable. Empties every
// Trash item out of the player's inventory in one interaction.
public class TrashCollectionSite : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData trashItem;

    public void Interact()
    {
        InventorySystem.Instance.RemoveAllOfItem(trashItem);
    }
}
