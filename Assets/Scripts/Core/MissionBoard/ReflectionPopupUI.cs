using UnityEngine;
using TMPro;

public class ReflectionPopupUI : MonoBehaviour
{
    public static ReflectionPopupUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI reflectionText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    private void OnEnable() => EventBus.OnMissionCompleted += HandleMissionCompleted;
    private void OnDisable() => EventBus.OnMissionCompleted -= HandleMissionCompleted;

    private void HandleMissionCompleted(int missionID, bool wasOptimal)
    {
        string text = wasOptimal
            ? "Good choice! Rebuilding the pulley system fixed the root cause."
            : "The patch worked for now, but the root cause remains...";

        reflectionText.text = text;
        gameObject.SetActive(true);
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ReflectionState);
    }

    public void OnDismiss()
    {
        gameObject.SetActive(false);
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ExploreState);
    }
}