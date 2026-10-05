using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD for the bridge builder: budget + status readouts, and buttons wired directly to
// BridgeBuilderSystem in the Inspector, same "buttons call straight into the owning system"
// pattern as TownNoticeUI/InfoBoardUI. Polls the system each frame rather than needing its own
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
    // Separate from statusText so the stakes (attempts left) read on their own next to Test,
    // rather than trailing the how-to hint.
    [SerializeField] private TextMeshProUGUI attemptsText;
    [SerializeField] private Button testButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button redoButton;
    // One entry per BridgeBuilderSystem.Materials index — authored 1:1 against it in the
    // Inspector, same fixed-array-of-buttons shape as PlanningUI.fiveWChoiceButtons.
    [SerializeField] private MaterialButton[] materialButtons;
    // Tints the selected material button's background so the current pick is always visible.
    [SerializeField] private Color selectedMaterialTint = new Color(1f, 0.85f, 0.45f);
    [SerializeField] private Color unselectedMaterialTint = Color.white;

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

            // Cost and strength on the button itself — the tradeoff the player is choosing between
            // shouldn't only be discoverable by placing a beam and watching the budget move.
            if (materialButtons[i].label != null)
                materialButtons[i].label.text =
                    $"{material.materialName}\n<size=70%>{material.costPerUnitLength:0}/m  Str {material.breakForce:0}</size>";
            if (materialButtons[i].icon != null && material.icon != null) materialButtons[i].icon.sprite = material.icon;
        }
    }

    private void Update()
    {
        if (system == null) return;

        budgetText.text = $"Budget: {system.BudgetUsed:0} / {system.Budget:0}";
        attemptsText.text = $"Attempts left: {system.RemainingAttempts}";

        bool building = system.Phase == BridgeBuilderSystem.BuildPhase.Building;
        testButton.interactable = building;
        resetButton.interactable = building;
        deleteButton.interactable = building && system.SelectedNode != null;
        undoButton.interactable = building && system.CanUndo;
        redoButton.interactable = building && system.CanRedo;

        BridgeMaterialData[] materials = system.Materials;
        for (int i = 0; i < materialButtons.Length; i++)
        {
            materialButtons[i].button.interactable = building;
            bool selected = i < materials.Length && materials[i] == system.SelectedMaterial;
            materialButtons[i].button.image.color = selected ? selectedMaterialTint : unselectedMaterialTint;
        }

        // The how-to hint is static Editor-authored text on its own TMP object, not driven here —
        // this label only reports the live phase.
        statusText.text = building ? string.Empty : "Testing...";
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
