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
    public GameObject choiceButtonGroup;

    [Header("5W Multiple Choice")]
    public GameObject fiveWChoiceGroup;
    public FiveWChoiceButton[] fiveWChoiceButtons;

    [Header("Settings")]
    public float typeSpeed = 0.05f;

    [Header("Audio")]
    [SerializeField] private AudioClip typeBlipClip;
    [SerializeField] private AudioClip wrongChoiceClip;

    private CanvasGroup canvasGroup;
    private MissionData currentMission;

    private bool isTyping = false;
    private string currentText = "";
    private Coroutine typingCoroutine;

    private readonly List<string> currentFiveWOptions = new List<string>();
    private string currentFiveWCorrect;

    private enum Stage { Trivial, Optimal, Who, What, Where, When, Why, Buttons }
    private Stage stage;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (displayText == null) Debug.LogError($"[{name}] displayText is not assigned!", this);
        if (nextArrow == null) Debug.LogError($"[{name}] nextArrow is not assigned!", this);
        if (choiceButtonGroup == null) Debug.LogError($"[{name}] choiceButtonGroup is not assigned!", this);
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

        choiceButtonGroup.SetActive(false);
        fiveWChoiceGroup.SetActive(false);
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
            FinishTyping();
            return;
        }

        if (IsFiveWStage(stage)) return; // advancing here is gated by picking the correct choice button

        nextArrow.SetActive(false);

        if (stage == Stage.Trivial)
        {
            stage = Stage.Optimal;
            displayText.text = "";
            StartTyping(currentMission.optimalSolutionName);
        }
        else if (stage == Stage.Optimal)
        {
            stage = Stage.Who;
            BeginFiveWStage();
        }
    }

    private static bool IsFiveWStage(Stage s) =>
        s == Stage.Who || s == Stage.What || s == Stage.Where || s == Stage.When || s == Stage.Why;

    private void FinishTyping()
    {
        if (IsFiveWStage(stage)) ShowFiveWChoices();
        else nextArrow.SetActive(true);
    }

    private void BeginFiveWStage()
    {
        fiveWChoiceGroup.SetActive(false);

        var (correct, distractors) = GetFiveWAnswerData(stage);
        if (string.IsNullOrWhiteSpace(correct))
        {
            Debug.LogError($"[{name}] Mission {currentMission.missionID} is missing a 5W answer for stage {stage}", this);
            return;
        }

        currentFiveWCorrect = correct;
        currentFiveWOptions.Clear();
        currentFiveWOptions.AddRange(distractors ?? System.Array.Empty<string>());
        currentFiveWOptions.Add(correct);
        Shuffle(currentFiveWOptions);

        displayText.text = "";
        StartTyping(BuildFiveWPrompt(stage, currentFiveWOptions));
    }

    private string GetFiveWQuestion(Stage s) => s switch
    {
        Stage.Who => "Who is affected by this problem?",
        Stage.What => "What is actually happening?",
        Stage.Where => "Where is this happening?",
        Stage.When => "When does this happen?",
        Stage.Why => "Why is this happening?",
        _ => throw new System.ArgumentOutOfRangeException(nameof(s), s, null)
    };

    private (string correct, string[] distractors) GetFiveWAnswerData(Stage s) => s switch
    {
        Stage.Who => (currentMission.who, currentMission.whoDistractors),
        Stage.What => (currentMission.what, currentMission.whatDistractors),
        Stage.Where => (currentMission.where, currentMission.whereDistractors),
        Stage.When => (currentMission.when, currentMission.whenDistractors),
        Stage.Why => (currentMission.why, currentMission.whyDistractors),
        _ => throw new System.ArgumentOutOfRangeException(nameof(s), s, null)
    };

    private string BuildFiveWPrompt(Stage s, List<string> options)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(GetFiveWQuestion(s));
        sb.Append("\n\n");
        for (int i = 0; i < options.Count; i++)
            sb.Append($"{i + 1}. {options[i]}\n");
        return sb.ToString().TrimEnd();
    }

    private void ShowFiveWChoices()
    {
        for (int i = 0; i < fiveWChoiceButtons.Length; i++)
        {
            var choiceButton = fiveWChoiceButtons[i];
            bool active = i < currentFiveWOptions.Count;
            choiceButton.button.gameObject.SetActive(active);
            if (!active) continue;

            int optionIndex = i;
            choiceButton.button.onClick.RemoveAllListeners();
            choiceButton.button.onClick.AddListener(() => OnFiveWChoiceSelected(choiceButton, optionIndex));
        }

        fiveWChoiceGroup.SetActive(true);
    }

    private void OnFiveWChoiceSelected(FiveWChoiceButton choiceButton, int optionIndex)
    {
        if (currentFiveWOptions[optionIndex] == currentFiveWCorrect)
        {
            fiveWChoiceGroup.SetActive(false);
            AdvanceFromFiveW();
        }
        else
        {
            AudioManager.Instance.PlaySFX(wrongChoiceClip);
            StartCoroutine(FlashWrong(choiceButton.label));
        }
    }

    private void AdvanceFromFiveW()
    {
        switch (stage)
        {
            case Stage.Who: stage = Stage.What; BeginFiveWStage(); break;
            case Stage.What: stage = Stage.Where; BeginFiveWStage(); break;
            case Stage.Where: stage = Stage.When; BeginFiveWStage(); break;
            case Stage.When: stage = Stage.Why; BeginFiveWStage(); break;
            case Stage.Why:
                stage = Stage.Buttons;
                displayText.text = "What approach would you like to choose?";
                choiceButtonGroup.SetActive(true);
                break;
        }
    }

    private static IEnumerator FlashWrong(TextMeshProUGUI label)
    {
        Color original = label.color;
        label.color = Color.red;
        yield return new WaitForSeconds(0.3f);
        label.color = original;
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

    public void SelectTrivial() => SelectSolution(SolutionType.Trivial);
    public void SelectOptimal() => SelectSolution(SolutionType.Optimal);

    private void SelectSolution(SolutionType choice)
    {
        EventBus.RaiseSolutionSelected(currentMission.missionID, choice);
        Hide();
    }
}
