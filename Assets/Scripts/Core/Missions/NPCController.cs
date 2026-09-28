using UnityEngine;

public class NPCController : MonoBehaviour, IInteractable
{
    public MissionData associatedMission;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

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
        // Disable our own collider so it stops competing with the minigame container's
        // collider at the same spot (e.g. Container_Trivial_M1's well-patch target) — see
        // RiverInteractable, which does the same via SetActive(false) for the same reason.
        GetComponent<Collider2D>().enabled = false;
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, associatedMission.missionID) < 0) return;
        missionCompleted = false;
        GetComponent<InteractionIndicator>()?.ResetVisibility();
        GetComponent<Collider2D>().enabled = true;
    }
}
