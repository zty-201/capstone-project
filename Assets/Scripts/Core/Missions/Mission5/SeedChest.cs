using UnityEngine;

// Mission 5's reward on the eastern fields: a one-time chest holding a Seed Pouch and a note from
// the fields' old farmer, foreshadowing the post-missions farming sandbox (see Docs/TODO.md).
// Needs no gating of its own — the far bank is only reachable once BridgeManager unlocks it on
// Mission 5's completion (either outcome). Same gate-on-inventory-success shape as BrickPickup:
// a full inventory leaves the chest closed so the pouch is never lost.
public class SeedChest : MonoBehaviour, IInteractable
{
    [SerializeField] private ItemData seedPouchItem;
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openedVisual;

    [TextArea(2, 4)]
    [SerializeField] private string[] noteLines =
    {
        "Inside the chest is a pouch of seeds, wrapped in a weathered note.",
        "\"Left these for whoever gets the crossing working again. The soil out here is good — it just needs someone who'll tend it properly.\"",
        "You got a Seed Pouch!"
    };

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    private bool opened;

    private void Awake()
    {
        if (seedPouchItem == null) Debug.LogError($"[{name}] seedPouchItem is not assigned!", this);
        if (closedVisual == null) Debug.LogError($"[{name}] closedVisual is not assigned!", this);
        if (openedVisual == null) Debug.LogError($"[{name}] openedVisual is not assigned!", this);

        closedVisual.SetActive(true);
        openedVisual.SetActive(false);
    }

    public void Interact()
    {
        if (opened) return;
        if (!InventorySystem.Instance.TryAddItem(seedPouchItem, 1)) return;

        opened = true;
        closedVisual.SetActive(false);
        openedVisual.SetActive(true);
        GetComponent<InteractionIndicator>()?.Hide();

        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(noteLines, null);
        GameManager.Instance.StateManager.ChangeState(GameStateType.Dialogue);
    }
}
