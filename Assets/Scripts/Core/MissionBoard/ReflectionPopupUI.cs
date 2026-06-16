using UnityEngine;
using TMPro;

public class ReflectionPopupUI : MonoBehaviour
{
    public static ReflectionPopupUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI reflectionText;

    [Header("Mission Data")]
    public MissionRegistry missionRegistry;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (reflectionText == null) Debug.LogError($"[{name}] reflectionText is not assigned!", this);
        if (missionRegistry == null) Debug.LogError($"[{name}] missionRegistry is not assigned!", this);

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
        MissionData data = missionRegistry.GetByID(missionID);

        if (data == null)
        {
            Debug.LogError($"[ReflectionPopupUI] No MissionData found for ID {missionID}");
            return;
        }

        reflectionText.text = wasOptimal ? data.optimalReflectionText : data.trivialReflectionText;
        ShowPanel();
        GameManager.Instance.StateManager.ChangeState(GameStateType.Reflection);
    }

    public void OnDismiss()
    {
        HidePanel();
        GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
    }
}
