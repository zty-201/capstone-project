using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzleState : IState
{
    public void Enter()
    {
        Debug.Log("<color=magenta>[State]</color> Puzzle Mode Active. Movement Locked.");
        // Later, you can fire an event here to tell the UI to show the bare-minimum puzzle overlay
    }

    public void Tick()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));

            // Notice the different event! A* Pathfinding will ignore this completely.
            EventBus.OnPuzzleClicked?.Invoke(worldPos);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=magenta>[State]</color> Exiting Puzzle Mode.");
        // Tell the UI to hide the puzzle overlay
    }
}