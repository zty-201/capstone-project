using UnityEngine;

public class TestSubscriber : MonoBehaviour
{
    // Subscribe to the event when this script is enabled
    private void OnEnable()
    {
        EventBus.OnDayEnded += RespondToDayEnding;
    }

    // ALWAYS unsubscribe when disabled to prevent memory leaks
    private void OnDisable()
    {
        EventBus.OnDayEnded -= RespondToDayEnding;
    }

    // The method that runs when the event is triggered
    private void RespondToDayEnding(int dayNumber)
    {
        Debug.Log($"<color=cyan>[Subscriber]</color> Heard that Day {dayNumber} ended! Updating town progression...");
    }
}