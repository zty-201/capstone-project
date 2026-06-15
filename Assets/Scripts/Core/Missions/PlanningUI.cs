using System.Collections;
using UnityEngine;
using TMPro;
using static MissionData;

public class PlanningUI : MonoBehaviour
{
    public static PlanningUI Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI displayText;     // shared text area for both solution names
    public GameObject choiceButtonGroup;    // parent containing both buttons, hidden until both texts shown

    [Header("Settings")]
    public float typeSpeed = 0.05f;

    private CanvasGroup canvasGroup;
    private MissionData currentMission;
    private Coroutine sequenceCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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

        if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
        sequenceCoroutine = StartCoroutine(RunSequence());
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator RunSequence()
    {
        // 1. Show trivial solution name
        yield return TypeText(currentMission.trivialSolutionName);
        yield return new WaitForSeconds(1f); // brief pause to let player read

        // 2. Clear, then show optimal solution name
        displayText.text = "";
        yield return TypeText(currentMission.optimalSolutionName);
        yield return new WaitForSeconds(1f);

        // 3. Reveal the two choice buttons ("1" = Trivial, "2" = Optimal)
        choiceButtonGroup.SetActive(true);
    }

    private IEnumerator TypeText(string sentence)
    {
        displayText.text = "";
        foreach (char letter in sentence.ToCharArray())
        {
            displayText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }
    }

    // Wired to button OnClick in Inspector
    public void SelectTrivial() => SelectSolution(SolutionType.Trivial);
    public void SelectOptimal() => SelectSolution(SolutionType.Optimal);

    private void SelectSolution(SolutionType choice)
    {
        EventBus.RaiseSolutionSelected(currentMission.missionID, choice);
        Hide();
    }
}