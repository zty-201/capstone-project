using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InfoBoardUI : MonoBehaviour
{
    public static InfoBoardUI Instance { get; private set; }

    [System.Serializable]
    public class InfoPage
    {
        public string title;
        [TextArea(3, 10)]
        public string body;
    }

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI pageCounterText;
    public Button nextButton;
    public Button previousButton;

    [Header("Content")]
    public InfoPage[] pages = new InfoPage[]
    {
        new InfoPage
        {
            title = "Welcome",
            body = "This village runs on Kaizen: continuous improvement. Villagers keep running into " +
                   "problems, and how you solve them shapes how the town grows. Look for a quick fix, or " +
                   "dig for the real root cause - the choice, and the consequences, are up to you."
        },
        new InfoPage
        {
            title = "Getting Around",
            body = "Click anywhere on the ground to walk there. Click on a villager, object, or building " +
                   "you're standing next to in order to interact with it - if you're too far away, your " +
                   "character will walk over first. Check the minimap in the top-right corner to see what's nearby."
        },
        new InfoPage
        {
            title = "Talking to Villagers",
            body = "Villagers with a problem have a prompt icon above their head. Talk to them to hear their " +
                   "complaint, then work through the 5 Whys - a chain of questions that digs past the symptom " +
                   "toward the real root cause."
        },
        new InfoPage
        {
            title = "The 5 Whys",
            body = "Each \"why\" offers a few possible answers - only one is correct. You'll move on no matter " +
                   "what you pick, so don't worry about getting stuck, but a wrong pick costs you. Look for the " +
                   "hint under each question if you're unsure. Answer all 5 correctly and you'll unlock the " +
                   "optimal, root-cause fix. Miss even one, and you're stuck patching the symptom instead - a " +
                   "quick fix that doesn't actually solve the problem."
        },
        new InfoPage
        {
            title = "Missions & the Mission Board",
            body = "Every villager's problem is a mission. Check the Mission Board in town any time to see " +
                   "which missions are open, resolved, or still \"Needs Review\" after a trivial fix."
        },
        new InfoPage
        {
            title = "The PDCA Cycle",
            body = "Every mission runs through Plan, Do, and Check: Plan while you dig through the 5 Whys and " +
                   "choose a fix, Do while you carry that fix out, and Check when you see the outcome and read " +
                   "what it means. Watch the tracker near the top of the screen to see which phase you're in."
        },
        new InfoPage
        {
            title = "Gold Coins & Trust",
            body = "Solve a mission's true root cause and you'll earn a Gold Coin - it always has its own " +
                   "inventory slot, so litter can never crowd it out. Spend coins at Town Hall to upgrade the " +
                   "whole village: 3 coins for the first upgrade, 2 more for the final one. Villagers also " +
                   "remember how you helped them: a trivial fix costs you their trust, while finding the real " +
                   "root cause earns it back."
        },
        new InfoPage
        {
            title = "Trash & Your Inventory",
            body = "Rubbish appears randomly around town. Click a piece to pick it up - it'll take a slot in " +
                   "your inventory until you drop it off at the Trash Collection Site. Let it pile up and " +
                   "you'll run out of room for mission items. A better-kept town makes less litter, so each " +
                   "Town Hall upgrade slows it down."
        },
        new InfoPage
        {
            title = "Quick Fixes & Town Hall",
            body = "Tackle missions in any order. A quick fix might look fine at first, but it won't last: " +
                   "after you finish your next mission, it breaks down and you'll need to take another look - " +
                   "with hints this time. Bring Gold Coins to Town Hall to upgrade the village. Upgrade it all " +
                   "the way and the village is complete."
        },
        new InfoPage
        {
            title = "What You'll Find Around Town",
            body = "- Villagers (NPCs): talk to them to start a mission.\n" +
                   "- Wells, rivers, and other mission sites: where a mission's minigame plays out once you've " +
                   "chosen a path.\n" +
                   "- Trash piles: pick these up before they crowd your inventory.\n" +
                   "- Trash Collection Site: empty every trash item out of your inventory at once.\n" +
                   "- Mission Board: check the status of every mission.\n" +
                   "- Town Hall: spend Gold Coins to upgrade the village."
        },
    };

    private CanvasGroup canvasGroup;
    private int currentPage;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        currentPage = 0;
        DisplayCurrentPage();
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // Wire directly to the Next/Previous buttons' OnClick() in the Inspector
    public void ShowNextPage()
    {
        if (currentPage >= pages.Length - 1) return;
        currentPage++;
        DisplayCurrentPage();
    }

    public void ShowPreviousPage()
    {
        if (currentPage <= 0) return;
        currentPage--;
        DisplayCurrentPage();
    }

    private void DisplayCurrentPage()
    {
        if (pages == null || pages.Length == 0)
        {
            Debug.LogError($"[{name}] No pages assigned.", this);
            return;
        }

        var page = pages[currentPage];
        titleText.text = page.title;
        bodyText.text = page.body;
        if (pageCounterText != null) pageCounterText.text = $"{currentPage + 1} / {pages.Length}";

        if (previousButton != null) previousButton.interactable = currentPage > 0;
        if (nextButton != null) nextButton.interactable = currentPage < pages.Length - 1;
    }
}
