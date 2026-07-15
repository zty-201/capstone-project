using UnityEngine;

public class RiverInteractable : MonoBehaviour, IInteractable
{
    public MissionData associatedMission;

    // OnMissionsNeedReview is subscribed in Awake/OnDestroy, not OnEnable/OnDisable: this
    // object disables itself in HandleSolutionSelected below, and an OnEnable/OnDisable
    // subscription would unsubscribe right then — leaving nothing listening to hear the
    // reactivate signal a later review request sends.
    private void Awake() => EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

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

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, associatedMission.missionID) < 0) return;
        gameObject.SetActive(true);
    }
}
