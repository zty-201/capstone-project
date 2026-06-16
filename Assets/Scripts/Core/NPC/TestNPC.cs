using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    public string[] dialogueLines;
    public MissionData associatedMission;

    public void Interact()
    {
        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(dialogueLines, associatedMission);
        GameManager.Instance.StateManager.ChangeState(GameStateType.Dialogue);
    }
}
