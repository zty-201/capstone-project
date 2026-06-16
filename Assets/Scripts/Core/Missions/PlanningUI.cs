using System.Collections;
using UnityEngine;
using TMPro;

public class PlanningUI : MonoBehaviour
{
    public static PlanningUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI displayText;
    public GameObject nextArrow;
    public GameObject choiceButtonGroup;

    [Header("Settings")]
    public float typeSpeed = 0.05f;

    private CanvasGroup canvasGroup;
    private MissionData currentMission;

    private bool isTyping = false;
    private string currentText = "";
    private Coroutine typingCoroutine;

    private enum Stage { Trivial, Optimal, Buttons }
    private Stage stage;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (displayText == null) Debug.LogError($"[{name}] displayText is not assigned!", this);
        if (nextArrow == null) Debug.LogError($"[{name}] nextArrow is not assigned!", this);
        if (choiceButtonGroup == null) Debug.LogError($"[{name}] choiceButtonGroup is not assigned!", this);

        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(MissionData mission)
    {
        currentMission = mission;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        choiceButtonGroup.SetActive(false);
        nextArrow.SetActive(false);

        stage = Stage.Trivial;
        StartTyping(currentMission.trivialSolutionName);
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // Called by PlanningState on left click
    public void OnAdvance()
    {
        if (stage == Stage.Buttons) return;

        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            displayText.text = currentText;
            isTyping = false;
            nextArrow.SetActive(true);
            return;
        }

        // Not typing — advance to next stage
        nextArrow.SetActive(false);

        if (stage == Stage.Trivial)
        {
            stage = Stage.Optimal;
            displayText.text = "";
            StartTyping(currentMission.optimalSolutionName);
        }
        else if (stage == Stage.Optimal)
        {
            stage = Stage.Buttons;
            displayText.text = "";
            nextArrow.SetActive(false);
            choiceButtonGroup.SetActive(true);
        }
    }

    private void StartTyping(string sentence)
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(sentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        currentText = sentence;
        isTyping = true;
        nextArrow.SetActive(false);
        displayText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            displayText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        nextArrow.SetActive(true);
    }

    public void SelectTrivial() => SelectSolution(SolutionType.Trivial);
    public void SelectOptimal() => SelectSolution(SolutionType.Optimal);

    private void SelectSolution(SolutionType choice)
    {
        EventBus.RaiseSolutionSelected(currentMission.missionID, choice);
        Hide();
    }
}
