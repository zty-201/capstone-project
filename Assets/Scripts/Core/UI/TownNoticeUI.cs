using UnityEngine;
using TMPro;

// Full-screen announcement panel for the two town-wide moments: the village being upgraded
// (OnTownUpgraded — the final upgrade doubles as the game's ending) and rushed fixes breaking
// down (OnMissionsNeedReview, raised by MissionReviewSystem). Dismissed via a UI Button wired to
// OnDismiss() in the Inspector.
public class TownNoticeUI : MonoBehaviour
{
    public static TownNoticeUI Instance { get; private set; }

    [System.Serializable]
    public class Notice
    {
        public string title;
        [TextArea(2, 4)] public string body;
    }

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    [SerializeField] private MissionRegistry missionRegistry;

    [Header("Text")]
    // Index = new town level - 1 (one entry per TownUpgradeSystem upgrade). The last one is the ending.
    [SerializeField] private Notice[] upgradeNotices =
    {
        new Notice
        {
            title = "The Village Is Improving!",
            body = "Coins earned by fixing problems at their root have gone back into the town. A tidier town stays tidy - litter will pile up far less often now."
        },
        new Notice
        {
            title = "A Village Built to Last",
            body = "Every problem solved at its root, every coin reinvested. This is what steady, continuous improvement builds."
        }
    };
    [SerializeField] private string breakdownTitle = "A Quick Fix Gave Way";
    // {0} = the broken-down missions' names.
    [TextArea(2, 4)]
    [SerializeField] private string breakdownBody = "{0}: the rushed fix didn't hold. Take another look - this time, find the root cause.";

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (titleText == null) Debug.LogError($"[{name}] titleText is not assigned!", this);
        if (subtitleText == null) Debug.LogError($"[{name}] subtitleText is not assigned!", this);
        if (missionRegistry == null) Debug.LogError($"[{name}] missionRegistry is not assigned!", this);

        canvasGroup = GetComponent<CanvasGroup>();
        HidePanel();
    }

    private void OnEnable()
    {
        EventBus.OnTownUpgraded += HandleTownUpgraded;
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDisable()
    {
        EventBus.OnTownUpgraded -= HandleTownUpgraded;
        EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;
    }

    private void HandleTownUpgraded(int level)
    {
        Notice notice = upgradeNotices[level - 1];
        Show(notice.title, notice.body);
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        string[] names = new string[missionIDs.Length];
        for (int i = 0; i < missionIDs.Length; i++)
            names[i] = missionRegistry.GetByID(missionIDs[i]).missionName;

        Show(breakdownTitle, string.Format(breakdownBody, string.Join(", ", names)));
    }

    private void Show(string title, string body)
    {
        titleText.text = title;
        subtitleText.text = body;
        ShowPanel();
        GameManager.Instance.StateManager.ChangeState(GameStateType.TownNotice);
    }

    public void OnDismiss()
    {
        HidePanel();
        GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
    }

    private void ShowPanel()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void HidePanel()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
