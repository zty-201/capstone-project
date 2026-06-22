using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    public MissionData associatedMission;

    public void Interact()
    {
        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(associatedMission.villagerComplaint, associatedMission);
        GameManager.Instance.StateManager.ChangeState(GameStateType.Dialogue);
    }
}
