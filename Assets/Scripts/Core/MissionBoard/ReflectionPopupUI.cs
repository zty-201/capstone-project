using UnityEngine;
using TMPro;

public class ReflectionPopupUI : MonoBehaviour
{
    public static ReflectionPopupUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI reflectionText;

    [Header("Mission Data")]
    public MissionData[] allMissions;

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
        MissionData data = System.Array.Find(allMissions, m => m.missionID == missionID);

        if (data == null)
        {
            Debug.LogError($"[ReflectionPopupUI] No MissionData found for ID {missionID}");
            return;
        }

        reflectionText.text = wasOptimal ? data.optimalReflectionText : data.trivialReflectionText;
        ShowPanel();
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ReflectionState);
    }

    public void OnDismiss()
    {
        HidePanel();
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ExploreState);
    }
}