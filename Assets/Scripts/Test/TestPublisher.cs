using UnityEngine;
using UnityEngine.InputSystem; // Added for the New Input System

public class TestPublisher : MonoBehaviour
{
    private int currentDay = 1;

    void Update()
    {
        // Check if the keyboard exists, then check if Space was pressed this frame
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log($"<color=yellow>[Publisher]</color> Triggering OnDayEnded for Day {currentDay}...");

            // The '?' checks if anyone is listening before invoking
            EventBus.OnDayEnded?.Invoke(currentDay);

            currentDay++;
        }
    }
}