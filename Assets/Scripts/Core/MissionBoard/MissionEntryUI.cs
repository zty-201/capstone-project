using UnityEngine;
using TMPro;

public class MissionEntryUI : MonoBehaviour
{
    public int missionID;
    public TextMeshProUGUI missionNameText;
    public TextMeshProUGUI statusText;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void MarkCompleted(bool wasOptimal)
    {
        canvasGroup.alpha = 0.4f;         // Grey out
        statusText.text = wasOptimal ? "Resolved" : "Needs Review";
    }
}