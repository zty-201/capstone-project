using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    public string[] dialogueLines;

    public void Interact()
    {
        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(dialogueLines);
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.DialogueState);
    }
}