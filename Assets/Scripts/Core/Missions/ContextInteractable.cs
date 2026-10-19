using UnityEngine;

// A narrative-only interaction point: shows dialogue then returns to Exploration.
// Does not start a mission or open PlanningUI (unlike NPCController / RiverInteractable).
// Optionally tied to a mission (resolvedByMissionID > 0): once that mission completes — either
// outcome, since every mission's world visual (e.g. RiverManager's flowing river) changes either
// way — it shows resolvedDialogueLines instead, and reverts to dialogueLines if a rushed fix
// breaks down (OnMissionsNeedReview), so the flavor text always matches the world's state.
public class ContextInteractable : MonoBehaviour, IInteractable
{
    [TextArea(2, 4)]
    public string[] dialogueLines;

    [Header("After Mission Resolved (optional)")]
    // 0 = not tied to any mission; dialogueLines are shown forever.
    [SerializeField] private int resolvedByMissionID;
    [TextArea(2, 4)]
    [SerializeField] private string[] resolvedDialogueLines;

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    private bool resolved;

    private void Awake()
    {
        if (resolvedByMissionID > 0 && (resolvedDialogueLines == null || resolvedDialogueLines.Length == 0))
            Debug.LogError($"[{name}] resolvedByMissionID is set but resolvedDialogueLines is empty!", this);
    }

    private void OnEnable()
    {
        EventBus.OnMissionCompleted += HandleMissionCompleted;
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDisable()
    {
        EventBus.OnMissionCompleted -= HandleMissionCompleted;
        EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;
    }

    public void Interact()
    {
        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(resolved ? resolvedDialogueLines : dialogueLines, null);
        GameManager.Instance.StateManager.ChangeState(GameStateType.Dialogue);
    }

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        if (resolvedByMissionID > 0 && missionID == resolvedByMissionID) resolved = true;
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (resolvedByMissionID > 0 && System.Array.IndexOf(missionIDs, resolvedByMissionID) >= 0) resolved = false;
    }
}
