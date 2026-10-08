using UnityEngine;

// Lies in the town from the start, outside the optimal container, but stays locked (Interact()
// is a no-op, no prompt icon) until the 5 Whys quiz routes this mission onto its Optimal path.
public class MachinePart : MonoBehaviour, IInteractable
{
    [SerializeField] private int missionID = 2;
    [SerializeField] private PartCollectionSystem collectionSystem;
    [SerializeField] private ItemData machinePartItem;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    private bool unlocked;

    private void Awake() => GetComponent<InteractionIndicator>().Hide();

    private void OnEnable() => EventBus.OnSolutionSelected += HandleSolutionSelected;
    private void OnDisable() => EventBus.OnSolutionSelected -= HandleSolutionSelected;

    private void HandleSolutionSelected(int selectedMissionID, SolutionType type)
    {
        if (selectedMissionID != missionID || type != SolutionType.Optimal) return;
        unlocked = true;
        GetComponent<InteractionIndicator>().ResetVisibility();
    }

    public void Interact()
    {
        if (!unlocked) return;

        // Same gate as TrashPiece/BrickPickup: only counts as collected if it actually fit,
        // so a full inventory leaves it on the ground rather than silently losing it.
        if (!InventorySystem.Instance.TryAddItem(machinePartItem, 1)) return;

        collectionSystem.OnPartCollected();
        gameObject.SetActive(false);
    }

    // A redo starts back at the quiz, so the part goes back to lying there locked.
    public void ResetPart()
    {
        unlocked = false;
        GetComponent<InteractionIndicator>().Hide();
        gameObject.SetActive(true);
    }
}
