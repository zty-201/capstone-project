using UnityEngine;

public class TownHallInteractable : MonoBehaviour, IInteractable
{
    [TextArea(2, 4)]
    [SerializeField] private string[] incompleteStageLines = new[]
    {
        "The town hall ledger isn't ready for review yet.",
        "Come back once every outstanding problem in the village has been addressed."
    };

    [TextArea(2, 4)]
    [SerializeField] private string[] allStagesCompleteLines = new[]
    {
        "The town hall has nothing left to review right now.",
        "Every stage on record has already been settled. The village thanks you for your work."
    };

    [TextArea(2, 4)]
    [SerializeField] private string[] trashOnGroundLines = new[]
    {
        "The ledger's ready, but the streets aren't.",
        "Clear every last scrap of litter before I can sign off on this stage."
    };

    public void Interact()
    {
        if (StageManager.Instance.AllStagesComplete)
        {
            ShowLines(allStagesCompleteLines);
            return;
        }

        if (!StageManager.Instance.AllMissionsCompleteForCurrentStage())
        {
            ShowLines(incompleteStageLines);
            return;
        }

        if (TrashSpawner.Instance.HasLiveTrash)
        {
            ShowLines(trashOnGroundLines);
            return;
        }

        StageManager.Instance.SubmitStage();
    }

    private void ShowLines(string[] lines)
    {
        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(lines, null);
        GameManager.Instance.StateManager.ChangeState(GameStateType.Dialogue);
    }
}
