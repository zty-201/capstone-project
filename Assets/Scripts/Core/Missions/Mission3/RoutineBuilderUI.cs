using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD for the routine builder: status/attempts/hint readouts, and a Submit button wired straight
// into FarmRoutineSystem in the Inspector — same "buttons call straight into the owning system"
// pattern as BridgeBuilderUI/DayCompleteUI/InfoBoardUI. Polls the system each frame rather than
// needing its own event, same reasoning as BridgeBuilderUI: this is a single dedicated UI for a
// single system, not a cross-domain listener (that's what EventBus is for elsewhere).
public class RoutineBuilderUI : MonoBehaviour
{
    [SerializeField] private FarmRoutineSystem system;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI attemptsText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Button submitButton;

    private void Update()
    {
        if (system == null) return;

        statusText.text = system.StatusMessage;
        attemptsText.text = $"Attempts left: {system.RemainingAttempts}";
        submitButton.interactable = system.CanRearrange;

        if (hintText != null)
        {
            bool show = system.ShowHint && !string.IsNullOrEmpty(system.HintText);
            hintText.gameObject.SetActive(show);
            if (show) hintText.text = system.HintText;
        }
    }

    // Wired to the Submit button's OnClick in the Inspector.
    public void OnSubmitPressed() => system.Submit();
}
