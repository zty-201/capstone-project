using UnityEngine;
using TMPro;

public class MissionEntryUI : MonoBehaviour
{
    public MissionData missionData;

    public TextMeshProUGUI missionNameText;
    public TextMeshProUGUI statusText;
    private CanvasGroup canvasGroup;

    private float originalAlpha;
    private string originalStatusText;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        missionNameText.text = missionData.missionName; // auto-populate on start

        originalAlpha = canvasGroup.alpha;
        originalStatusText = statusText.text;
    }

    public void MarkCompleted(bool wasOptimal)
    {
        canvasGroup.alpha = 0.4f;
        statusText.text = wasOptimal ? "Resolved" : "Needs Review";
    }

    public void ResetVisual()
    {
        canvasGroup.alpha = originalAlpha;
        statusText.text = originalStatusText;
    }
}