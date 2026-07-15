using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlanningUI : MonoBehaviour
{
    public static PlanningUI Instance { get; private set; }

    [System.Serializable]
    public class FiveWChoiceButton
    {
        public Button button;
        public TextMeshProUGUI label;
    }

    [Header("UI References")]
    public TextMeshProUGUI displayText;
    public GameObject nextArrow;

    [Header("5 Whys Multiple Choice")]
    public GameObject fiveWChoiceGroup;
    public FiveWChoiceButton[] fiveWChoiceButtons;
    public TextMeshProUGUI hintText;

    [Header("Settings")]
    public float typeSpeed = 0.05f;
    public float choiceFeedbackDelay = 0.4f;

    [Header("Audio")]
    [SerializeField] private AudioClip typeBlipClip;
    [SerializeField] private AudioClip wrongChoiceClip;

    private CanvasGroup canvasGroup;
    private MissionData currentMission;

    private bool isTyping = false;
    private string currentText = "";
    private Coroutine typingCoroutine;

    private readonly List<string> currentOptions = new List<string>();
    private string currentCorrectAnswer;

    private int whyIndex;
    private int correctCount;
    private bool outcomeIsOptimal;

    private enum Stage { Trivial, Optimal, Why, Outcome }
    private Stage stage;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (displayText == null) Debug.LogError($"[{name}] displayText is not assigned!", this);
        if (nextArrow == null) Debug.LogError($"[{name}] nextArrow is not assigned!", this);
        if (fiveWChoiceGroup == null) Debug.LogError($"[{name}] fiveWChoiceGroup is not assigned!", this);

        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(MissionData mission)
    {
        currentMission = mission;

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        fiveWChoiceGroup.SetActive(false);
        if (hintText != null) hintText.gameObject.SetActive(false);
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
        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            displayText.text = currentText;
            isTyping = false;
            FinishTyping();
            return;
        }

        if (stage == Stage.Why) return; // advancing here is gated by picking a choice button

        nextArrow.SetActive(false);

        switch (stage)
        {
            case Stage.Trivial:
                stage = Stage.Optimal;
                displayText.text = "";
                StartTyping(currentMission.optimalSolutionName);
                break;

            case Stage.Optimal:
                stage = Stage.Why;
                whyIndex = 0;
                correctCount = 0;
                if (hintText != null) hintText.gameObject.SetActive(StageManager.Instance.IsMissionUnderReview(currentMission.missionID));
                BeginWhyStage();
                break;

            case Stage.Outcome:
                SelectSolution(outcomeIsOptimal ? SolutionType.Optimal : SolutionType.Trivial);
                break;
        }
    }

    private void FinishTyping()
    {
        if (stage == Stage.Why) ShowWhyChoices();
        else nextArrow.SetActive(true);
    }

    private void BeginWhyStage()
    {
        fiveWChoiceGroup.SetActive(false);

        if (currentMission.fiveWhys == null || whyIndex >= currentMission.fiveWhys.Length)
        {
            Debug.LogError($"[{name}] Mission {currentMission.missionID} is missing 5 Whys stage {whyIndex}", this);
            return;
        }

        var data = currentMission.fiveWhys[whyIndex];
        if (string.IsNullOrWhiteSpace(data.correctAnswer))
        {
            Debug.LogError($"[{name}] Mission {currentMission.missionID} is missing a Why answer for stage {whyIndex}", this);
            return;
        }

        currentCorrectAnswer = data.correctAnswer;
        currentOptions.Clear();

        var distractors = data.distractors ?? System.Array.Empty<string>();
        var excluded = StageManager.Instance.GetExcludedDistractors(currentMission.missionID, whyIndex);
        foreach (string distractor in distractors)
            if (excluded == null || !excluded.Contains(distractor))
                currentOptions.Add(distractor);

        // Floor guard: never let a redo collapse a question down to just the correct answer.
        if (currentOptions.Count == 0 && distractors.Length > 0)
            currentOptions.Add(distractors[0]);

        currentOptions.Add(data.correctAnswer);
        Shuffle(currentOptions);

        if (hintText != null) hintText.text = data.hint;

        displayText.text = "";
        StartTyping(BuildWhyPrompt(data.question, currentOptions));
    }

    private string BuildWhyPrompt(string question, List<string> options)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Why #{whyIndex + 1}: ").Append(question);
        sb.Append("\n\n");
        for (int i = 0; i < options.Count; i++)
            sb.Append($"{i + 1}. {options[i]}\n");
        return sb.ToString().TrimEnd();
    }

    private void ShowWhyChoices()
    {
        for (int i = 0; i < fiveWChoiceButtons.Length; i++)
        {
            var choiceButton = fiveWChoiceButtons[i];
            bool active = i < currentOptions.Count;
            choiceButton.button.gameObject.SetActive(active);
            if (!active) continue;

            choiceButton.button.interactable = true;
            int optionIndex = i;
            choiceButton.button.onClick.RemoveAllListeners();
            choiceButton.button.onClick.AddListener(() => OnWhyChoiceSelected(choiceButton, optionIndex));
        }

        fiveWChoiceGroup.SetActive(true);
    }

    private void OnWhyChoiceSelected(FiveWChoiceButton choiceButton, int optionIndex)
    {
        foreach (var choice in fiveWChoiceButtons)
            choice.button.interactable = false;

        bool isCorrect = currentOptions[optionIndex] == currentCorrectAnswer;
        if (isCorrect)
        {
            correctCount++;
        }
        else
        {
            AudioManager.Instance.PlaySFX(wrongChoiceClip);
            StageManager.Instance.RecordWrongAnswer(currentMission.missionID, whyIndex, currentOptions[optionIndex]);
        }

        StartCoroutine(FlashChoiceThenAdvance(choiceButton.label, isCorrect));
    }

    private IEnumerator FlashChoiceThenAdvance(TextMeshProUGUI label, bool isCorrect)
    {
        Color original = label.color;
        label.color = isCorrect ? Color.green : Color.red;
        yield return new WaitForSeconds(choiceFeedbackDelay);
        label.color = original;

        fiveWChoiceGroup.SetActive(false);

        whyIndex++;
        if (whyIndex < currentMission.fiveWhys.Length)
            BeginWhyStage();
        else
            FinishFiveWhys();
    }

    private void FinishFiveWhys()
    {
        stage = Stage.Outcome;
        if (hintText != null) hintText.gameObject.SetActive(false);

        outcomeIsOptimal = correctCount >= currentMission.fiveWhys.Length;
        displayText.text = "";
        StartTyping(outcomeIsOptimal
            ? "You've traced the true root cause. Time to fix this properly."
            : "You lost the thread partway through... this will need a quick fix for now.");
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
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
            if (!char.IsWhiteSpace(letter)) AudioManager.Instance.PlaySFX(typeBlipClip);
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        FinishTyping();
    }

    private void SelectSolution(SolutionType choice)
    {
        EventBus.RaiseSolutionSelected(currentMission.missionID, choice);
        Hide();
    }
}
