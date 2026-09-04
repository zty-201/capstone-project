using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Thin HUD readout for the bridge builder: plank counter + Test/Reset buttons wired directly to
// BridgeBuilderSystem in the Inspector, same "buttons call straight into the owning system"
// pattern as DayCompleteUI/InfoBoardUI. Polls the system each frame rather than needing its own
// event — this is a single dedicated UI for a single system, not a cross-domain listener (that's
// what EventBus is for elsewhere, e.g. MissionDirectoryUI).
public class BridgeBuilderUI : MonoBehaviour
{
    [SerializeField] private BridgeBuilderSystem system;
    [SerializeField] private TextMeshProUGUI plankCountText;
    [SerializeField] private Button testButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private void Update()
    {
        if (system == null) return;

        plankCountText.text = $"Planks: {system.RemainingPlanks}";

        bool building = system.Phase == BridgeBuilderSystem.BuildPhase.Building;
        testButton.interactable = building;
        resetButton.interactable = building;
        statusText.text = building ? "Connect the nodes, then test the bridge." : "Testing...";
    }

    // Wired to the Test button's OnClick in the Inspector.
    public void OnTestPressed() => system.StartTest();

    // Wired to the Reset button's OnClick in the Inspector.
    public void OnResetPressed() => system.ResetBridge();
}
