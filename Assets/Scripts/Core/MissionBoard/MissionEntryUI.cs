using UnityEngine;
using TMPro;

public class MissionEntryUI : MonoBehaviour
{
    public MissionData missionData;

    public TextMeshProUGUI missionNameText;
    public TextMeshProUGUI statusText;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        missionNameText.text = missionData.missionName; // auto-populate on start
    }

    public void MarkCompleted(bool wasOptimal)
    {
        canvasGroup.alpha = 0.4f;
        statusText.text = wasOptimal ? "Resolved" : "Needs Review";
    }
}