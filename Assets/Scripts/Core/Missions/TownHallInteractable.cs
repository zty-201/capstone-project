using UnityEngine;

// The town's upgrade shop: interacting spends Gold Coins on the next TownUpgradeSystem upgrade if
// the player can afford it (TownNoticeUI announces it), otherwise explains what's still needed.
public class TownHallInteractable : MonoBehaviour, IInteractable
{
    // {0} = coins still needed for the next upgrade.
    [TextArea(2, 4)]
    [SerializeField] private string[] notEnoughCoinsLines = new[]
    {
        "Welcome! The town's improvement fund is open.",
        "Bring {0} more gold coin(s) - earned by fixing problems at their root - and we'll put them to work."
    };

    [TextArea(2, 4)]
    [SerializeField] private string[] villageCompleteLines = new[]
    {
        "Every problem solved at its root, every coin reinvested.",
        "There's nothing left to build - the village thanks you for your work."
    };

    [Header("Audio")]
    [SerializeField] private AudioClip interactSfx;
    public AudioClip InteractSfx => interactSfx;

    public void Interact()
    {
        TownUpgradeSystem upgrades = TownUpgradeSystem.Instance;

        if (upgrades.IsMaxLevel)
        {
            ShowLines(villageCompleteLines);
            return;
        }

        if (!upgrades.CanAffordNextUpgrade())
        {
            ShowLines(FormatLines(notEnoughCoinsLines, upgrades.CoinsNeededForNextUpgrade));
            return;
        }

        upgrades.PurchaseNextUpgrade();
    }

    private static string[] FormatLines(string[] lines, int coinsNeeded)
    {
        string[] formatted = new string[lines.Length];
        for (int i = 0; i < lines.Length; i++) formatted[i] = string.Format(lines[i], coinsNeeded);
        return formatted;
    }

    private void ShowLines(string[] lines)
    {
        DialogueManager.Instance.gameObject.SetActive(true);
        DialogueManager.Instance.StartDialogue(lines, null);
        GameManager.Instance.StateManager.ChangeState(GameStateType.Dialogue);
    }
}
