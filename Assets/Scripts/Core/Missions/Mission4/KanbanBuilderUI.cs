using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD for the Kanban panel: a status readout and a Run Day button wired straight into
// KanbanBuilderSystem in the Inspector — same "buttons call straight into the owning system"
// pattern as RoutineBuilderUI/BridgeBuilderUI. Polls the system each frame rather than needing
// its own event, same reasoning as RoutineBuilderUI: this is a single dedicated UI for a single
// system, not a cross-domain listener.
public class KanbanBuilderUI : MonoBehaviour
{
    [SerializeField] private KanbanBuilderSystem system;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button runDayButton;

    private void Update()
    {
        if (system == null) return;

        statusText.text = system.StatusMessage;
        runDayButton.interactable = system.CanConfigure;
    }

    // Wired to the Run Day button's OnClick in the Inspector.
    public void OnRunDayPressed() => system.RunDay();
}
