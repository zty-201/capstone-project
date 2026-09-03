using UnityEngine;

// Consequence of the trivial rope-crossing (Mission 5): a villager who trusted the rickety
// bridge ends up in the water and needs a hand out. Purely a narrative/world-state beat — same
// walk-up-and-click, dialogue-with-no-MissionData shape as ContextInteractable — and doesn't
// gate anything mechanically (Town Hall submission only checks trash/coins/mission outcomes,
// not this).
//
// Starts inactive in the scene (author it that way in the Editor, standing/floating in the
// water near the far end of the rope bridge). Subscribed in Awake/OnDestroy rather than
// OnEnable/OnDisable: an inactive GameObject's OnEnable never fires, and this needs to hear
// OnMissionCompleted while still inactive in order to know when to reveal itself. The `rescued`
// guard makes this strictly one-time — a later trivial redo re-shows the same event only if it
// hasn't been resolved yet, never a second time after it has.
public class StrandedVillager : MonoBehaviour, IInteractable
{
    [SerializeField] private int missionID = 5;
    [TextArea(2, 4)]
    [SerializeField] private string[] rescueDialogue;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    private bool rescued;

    private void Awake() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDestroy() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    private void HandleMissionCompleted(int id, bool wasOptimal)
    {
        if (id != missionID || wasOptimal || rescued) return;
        gameObject.SetActive(true);
    }

    public void Interact()
    {
        rescued = true;
        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(rescueDialogue, null);
        GameManager.Instance.StateManager.ChangeState(GameStateType.Dialogue);
        gameObject.SetActive(false);
    }
}
