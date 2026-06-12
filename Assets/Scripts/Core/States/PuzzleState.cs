using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzleState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[PuzzleState]</color> Entered: Playing the Pipe Puzzle.");

        // Turn the pipes ON when the state starts
        if (GameManager.Instance.puzzleContainer != null)
        {
            GameManager.Instance.puzzleContainer.SetActive(true);
        }
    }

    public void Tick()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            float distToPlane = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, distToPlane));

            EventBus.OnPuzzleClicked?.Invoke(worldPos);
        }

        // Exit puzzle on 'X'
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[PuzzleState]</color> 'X' pressed. Transitioning back to Exploration State...");
            GameManager.Instance.StateManager.ChangeState(GameManager.Instance.ExploreState);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[PuzzleState]</color> Exited: Returning to town.");

        // Turn the pipes OFF when leaving the state
        if (GameManager.Instance.puzzleContainer != null)
        {
            GameManager.Instance.puzzleContainer.SetActive(false);
        }
    }
}