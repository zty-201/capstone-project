using UnityEngine;

// A narrative-only interaction point: shows dialogue then returns to Exploration.
// Does not start a mission or open PlanningUI (unlike NPCController / RiverInteractable).
public class ContextInteractable : MonoBehaviour, IInteractable
{
    [TextArea(2, 4)]
    public string[] dialogueLines;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(dialogueLines, null);
        GameManager.Instance.StateManager.ChangeState(GameStateType.Dialogue);
    }
}
