using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI dialogueText;
    public GameObject nextArrow;

    [Header("Settings")]
    public float typeSpeed = 0.05f;

    private Queue<string> sentences;
    private string currentSentence;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private MissionData pendingMission;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        sentences = new Queue<string>();
        nextArrow.SetActive(false);

        // Now safe to hide — singleton is already assigned
        gameObject.SetActive(false);
    }

    public void StartDialogue(string[] newSentences, MissionData mission) // CHANGED signature
    {
        pendingMission = mission;

        if (newSentences == null || newSentences.Length == 0)
        {
            Debug.LogError("[DialogueManager] StartDialogue called with no lines.");
            return;
        }

        sentences.Clear();
        foreach (string sentence in newSentences)
            sentences.Enqueue(sentence);

        DisplayNextSentence();
    }

    public void OnAdvanceDialogue()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentSentence;
            isTyping = false;
            nextArrow.SetActive(true);
        }
        else
        {
            DisplayNextSentence();
        }
    }

    private void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentSentence = sentences.Dequeue();
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(currentSentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        nextArrow.SetActive(false);
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        nextArrow.SetActive(true);
    }

    private void EndDialogue()
    {
        Debug.Log("<color=green>[DialogueManager]</color> Conversation ended.");
        gameObject.SetActive(false);

        if (pendingMission != null)
        {
            PlanningUI.Instance.Show(pendingMission);
            GameManager.Instance.StateManager.ChangeState(GameManager.Instance.PlanningState);
        }
        else
        {
            GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ExploreState);
        }
    }
}