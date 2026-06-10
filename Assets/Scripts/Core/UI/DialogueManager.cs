using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Requires TextMeshPro

public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI dialogueText;
    public GameObject nextArrow;

    [Header("Settings")]
    public float typeSpeed = 0.05f;

    private Queue<string> sentences;
    private string currentSentence;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    private void Awake()
    {
        sentences = new Queue<string>();
        nextArrow.SetActive(false);
    }

    // Call this from your NPC when the player interacts with them
    public void StartDialogue(string[] newSentences)
    {
        sentences.Clear();

        foreach (string sentence in newSentences)
        {
            sentences.Enqueue(sentence);
        }

        DisplayNextSentence();
    }

    // Call this when the player clicks/presses a button to continue
    public void OnAdvanceDialogue()
    {
        if (isTyping)
        {
            // If the player clicks WHILE typing, skip the effect and show the whole sentence instantly
            StopCoroutine(typingCoroutine);
            dialogueText.text = currentSentence;
            isTyping = false;
            nextArrow.SetActive(true);
        }
        else
        {
            // If the text is done, move to the next sentence
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

        // Stop any previous typing and start fresh
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(currentSentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        nextArrow.SetActive(false); // Hide arrow while typing
        dialogueText.text = "";

        // Loop through each character in the string
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typeSpeed); // Wait a fraction of a second
        }

        isTyping = false;
        nextArrow.SetActive(true); // Show arrow when typing is complete!
    }

    private void EndDialogue()
    {
        Debug.Log("<color=green>[DialogueManager]</color> Conversation Ended.");
        gameObject.SetActive(false);
        GameManager.Instance.StateManager.ChangeState(GameManager.Instance.PuzzleState);
    }
}