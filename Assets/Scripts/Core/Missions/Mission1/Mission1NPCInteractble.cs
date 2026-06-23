using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    public MissionData associatedMission;

    private void OnEnable() => EventBus.OnSolutionSelected += HandleSolutionSelected;
    private void OnDisable() => EventBus.OnSolutionSelected -= HandleSolutionSelected;

    public void Interact()
    {
        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(associatedMission.villagerComplaint, associatedMission);
        GameManager.Instance.StateManager.ChangeState(GameStateType.Dialogue);
    }

    private void HandleSolutionSelected(int missionID, SolutionType type)
    {
        if (missionID != associatedMission.missionID) return;
        gameObject.SetActive(false);
    }
}
