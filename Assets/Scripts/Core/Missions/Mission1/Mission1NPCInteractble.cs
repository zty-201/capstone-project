using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    public MissionData associatedMission;

    private bool missionCompleted;

    private void OnEnable()
    {
        EventBus.OnSolutionSelected += HandleSolutionSelected;
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDisable()
    {
        EventBus.OnSolutionSelected -= HandleSolutionSelected;
        EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;
    }

    public void Interact()
    {
        if (missionCompleted) return;

        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(associatedMission.villagerComplaint, associatedMission);
        GameManager.Instance.StateManager.ChangeState(GameStateType.Dialogue);
    }

    private void HandleSolutionSelected(int missionID, SolutionType type)
    {
        if (missionID != associatedMission.missionID) return;
        missionCompleted = true;
        GetComponent<InteractionIndicator>()?.Hide();
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, associatedMission.missionID) < 0) return;
        missionCompleted = false;
        GetComponent<InteractionIndicator>()?.ResetVisibility();
    }
}
