using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    public string[] dialogueLines;
    public DialogueManager dialogueManager;
    public void Interact()
    {
        Debug.Log("<color=yellow>[NPC]</color> Farmer says: 'Please fix my pipes!'");
        // Turn on the UI Canvas
        dialogueManager.gameObject.SetActive(true);

        // Send the text to the manager
        dialogueManager.StartDialogue(dialogueLines);
    }
}