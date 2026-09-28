using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD for the Kanban panel: a status readout, a Run Day button, and a day-progress readout while
// a run is in flight — wired straight into KanbanBuilderSystem in the Inspector, same "buttons
// call straight into the owning system" pattern as RoutineBuilderUI/BridgeBuilderUI. Polls the
// system each frame rather than needing its own event, same reasoning as RoutineBuilderUI: this
// is a single dedicated UI for a single system, not a cross-domain listener.
//
// The panel's static "how to read this" instruction line (e.g. "Each bar is a stall's stock.
// Drag the pin to set when it should reorder...") is authored directly as its own always-on
// TextMeshProUGUI in the Editor, not a field here — it never changes at runtime, so there's
// nothing for this script to drive.
public class KanbanBuilderUI : MonoBehaviour
{
    [SerializeField] private KanbanBuilderSystem system;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button runDayButton;
    [SerializeField] private TextMeshProUGUI dayProgressText;

    private void Update()
    {
        if (system == null) return;

        statusText.text = system.StatusMessage;
        runDayButton.interactable = system.CanConfigure;

        if (dayProgressText != null)
        {
            dayProgressText.gameObject.SetActive(system.IsSimulating);
            if (system.IsSimulating)
                dayProgressText.text = $"Day: {Mathf.RoundToInt(system.DayProgress01 * 100f)}%";
        }
    }

    // Wired to the Run Day button's OnClick in the Inspector.
    public void OnRunDayPressed() => system.RunDay();
}
