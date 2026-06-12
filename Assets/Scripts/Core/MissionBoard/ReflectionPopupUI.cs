using UnityEngine;
using TMPro;

public class ReflectionPopupUI : MonoBehaviour
{
    public static ReflectionPopupUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI reflectionText;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        canvasGroup = GetComponent<CanvasGroup>();
        HidePanel();
    }

    private void HidePanel()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ShowPanel()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        string text = wasOptimal
            ? "Good choice! Rebuilding the pulley system fixed the root cause."
            : "The patch worked for now, but the root cause remains...";

        reflectionText.text = text;
        ShowPanel();
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ReflectionState);
    }

    public void OnDismiss()
    {
        HidePanel();
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ExploreState);
    }
}