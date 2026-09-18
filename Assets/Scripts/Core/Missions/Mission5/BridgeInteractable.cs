using UnityEngine;

// Starts Mission 5 (design doc's "Advanced Mission 3: Bridge Building"). Lives on the broken
// bridge itself, not an NPC — same pattern as RiverInteractable (Mission 2) and the well
// (Mission 1): the thing that's actually broken is what the player clicks to open dialogue.
public class BridgeInteractable : MonoBehaviour, IInteractable
{
    public MissionData associatedMission;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    // Subscribed in Awake/OnDestroy, not OnEnable/OnDisable: this object disables itself in
    // HandleSolutionSelected below, and an OnEnable/OnDisable subscription would unsubscribe
    // right then — leaving nothing listening to hear a later review request's reactivate signal.
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
