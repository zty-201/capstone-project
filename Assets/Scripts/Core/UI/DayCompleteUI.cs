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
        int satisfaction = TownSatisfactionSystem.Instance.CurrentSatisfaction;
        titleText.text = $"Day {day} Complete!";
        subtitleText.text = BuildSubtitle(satisfaction);
        ShowPanel();
        GameManager.Instance.StateManager.ChangeState(GameStateType.DayComplete);
    }

    // StageManager raises this BEFORE retracting any newly-flagged mission's trivial reward, so
    // CurrentSatisfaction here reflects the score the player actually earned this attempt — the
    // retraction (dropping the trivial mission's credit until it's redone) happens right after.
    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        int satisfaction = TownSatisfactionSystem.Instance.CurrentSatisfaction;
        string missionWord = missionIDs.Length == 1 ? "mission needs" : "missions need";
        titleText.text = "Needs Review";
        subtitleText.text = $"Town Satisfaction: {satisfaction}/100\n{missionIDs.Length} {missionWord} another look — its credit is on hold until it's resolved.";
        ShowPanel();
        GameManager.Instance.StateManager.ChangeState(GameStateType.DayComplete);
    }

    private string BuildSubtitle(int satisfaction)
    {
        if (satisfaction >= 80)
            return $"Town Satisfaction: {satisfaction}/100\nThe village is thriving under your care.";
        if (satisfaction >= 50)
            return $"Town Satisfaction: {satisfaction}/100\nProgress is being made, but there's more to improve.";
        return $"Town Satisfaction: {satisfaction}/100\nThe village is struggling. Focus on root causes tomorrow.";
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
