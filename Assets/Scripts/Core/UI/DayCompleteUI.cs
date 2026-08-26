using UnityEngine;
using TMPro;

public class DayCompleteUI : MonoBehaviour
{
    public static DayCompleteUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (titleText == null) Debug.LogError($"[{name}] titleText is not assigned!", this);
        if (subtitleText == null) Debug.LogError($"[{name}] subtitleText is not assigned!", this);

        canvasGroup = GetComponent<CanvasGroup>();
        HidePanel();
    }

    private void OnEnable()
    {
        EventBus.OnDayCompleted += HandleDayCompleted;
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDisable()
    {
        EventBus.OnDayCompleted -= HandleDayCompleted;
        EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;
    }

    private void HandleDayCompleted(int day)
    {
        titleText.text = $"Day {day} Complete!";
        subtitleText.text = "Every problem this stage was solved at the root, and the treasury's ledger is settled.";
        ShowPanel();
        GameManager.Instance.StateManager.ChangeState(GameStateType.DayComplete);
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        string missionWord = missionIDs.Length == 1 ? "mission needs" : "missions need";
        titleText.text = "Needs Review";
        subtitleText.text = $"{missionIDs.Length} {missionWord} another look before Town Hall will accept this stage.";
        ShowPanel();
        GameManager.Instance.StateManager.ChangeState(GameStateType.DayComplete);
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
