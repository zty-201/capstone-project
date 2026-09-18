using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD for the bridge builder: budget + status readouts, and buttons wired directly to
// BridgeBuilderSystem in the Inspector, same "buttons call straight into the owning system"
// pattern as DayCompleteUI/InfoBoardUI. Polls the system each frame rather than needing its own
// event — this is a single dedicated UI for a single system, not a cross-domain listener (that's
// what EventBus is for elsewhere, e.g. MissionDirectoryUI).
public class BridgeBuilderUI : MonoBehaviour
{
    [System.Serializable]
    public class MaterialButton
    {
        public Button button;
        public TextMeshProUGUI label;
        // Optional — leave unassigned if this button doesn't show a material icon.
        public Image icon;
    }

    [SerializeField] private BridgeBuilderSystem system;
    [SerializeField] private TextMeshProUGUI budgetText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button testButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;
    // One entry per BridgeBuilderSystem.Materials index — authored 1:1 against it in the
    // Inspector, same fixed-array-of-buttons shape as PlanningUI.fiveWChoiceButtons.
    [SerializeField] private MaterialButton[] materialButtons;

    private void Start()
    {
        BridgeMaterialData[] materials = system.Materials;

        for (int i = 0; i < materialButtons.Length; i++)
        {
            int index = i; // capture by value, not by the loop variable
            materialButtons[i].button.onClick.AddListener(() => system.SelectMaterial(index));

            // Labels/icons are driven from BridgeMaterialData here rather than typed by hand
            // per button, so they can't drift out of sync with whatever's actually assigned to
            // BridgeBuilderSystem.materials.
            if (materials == null || index >= materials.Length) continue;
            BridgeMaterialData material = materials[index];

            if (materialButtons[i].label != null) materialButtons[i].label.text = material.materialName;
            if (materialButtons[i].icon != null && material.icon != null) materialButtons[i].icon.sprite = material.icon;
        }
    }

    private void Update()
    {
        if (system == null) return;

        budgetText.text = $"Budget: {system.RemainingBudget:0.0}";

        bool building = system.Phase == BridgeBuilderSystem.BuildPhase.Building;
        testButton.interactable = building;
        resetButton.interactable = building;
        deleteButton.interactable = building && system.SelectedNode != null;
        undoButton.interactable = building && system.CanUndo;
        redoButton.interactable = building && system.CanRedo;

        foreach (var materialButton in materialButtons)
            materialButton.button.interactable = building;

        statusText.text = building
            ? $"Drag to place a beam. Attempts left: {system.RemainingAttempts}"
            : "Testing...";
    }

    // Wired to the Test button's OnClick in the Inspector.
    public void OnTestPressed() => system.StartTest();

    // Wired to the Reset button's OnClick in the Inspector.
    public void OnResetPressed() => system.ResetBridge();

    // Wired to the Delete button's OnClick in the Inspector.
    public void OnDeletePressed() => system.DeleteSelectedNode();

    // Wired to the Undo button's OnClick in the Inspector.
    public void OnUndoPressed() => system.Undo();

    // Wired to the Redo button's OnClick in the Inspector.
    public void OnRedoPressed() => system.Redo();
}
