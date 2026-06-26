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

    private void OnEnable() => EventBus.OnDayCompleted += HandleDayCompleted;
    private void OnDisable() => EventBus.OnDayCompleted -= HandleDayCompleted;

    private void HandleDayCompleted(int day)
    {
        titleText.text = $"Day {day} Complete!";
        subtitleText.text = "You chose the optimal solution for every problem.\nThe village is on the path to lasting improvement.";
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
