using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    public string[] dialogueLines;
    public MissionData associatedMission; // NEW

    public void Interact()
    {
        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(dialogueLines, associatedMission); // CHANGED
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.DialogueState);
    }
}