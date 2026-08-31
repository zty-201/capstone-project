using UnityEngine;

// World pickup for the trivial path's fetch step: same shape as MachinePart (Mission 2
// optimal) and TrashPiece — only removes itself once the item actually fit in the inventory,
// so a full inventory leaves it on the ground untouched rather than silently losing it.
public class BrickPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private int missionID = 1;
    [SerializeField] private ItemData brickItem;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        if (!InventorySystem.Instance.TryAddItem(brickItem, 1)) return;

        gameObject.SetActive(false);
        EventBus.RaiseObjectiveProgress(missionID, SolutionType.Trivial, 1, 0, 0);
    }

    public void ResetPickup() => gameObject.SetActive(true);
}
