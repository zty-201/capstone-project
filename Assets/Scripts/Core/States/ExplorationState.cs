using UnityEngine;
using UnityEngine.InputSystem;

public class ExplorationState : IState
{
    public void Enter()
    {
        Debug.Log("<color=green>[State]</color> Exploration Mode Active. Movement Unlocked.");
    }

    public void Tick()
    {
        // Poll for input exactly like you used to
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            float distToPlane = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, distToPlane));

            // Broadcast the click intent to the EventBus
            EventBus.RaiseMapClicked(worldPos);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=green>[State]</color> Exiting Exploration Mode.");
    }
}