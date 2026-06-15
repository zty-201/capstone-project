using UnityEngine;
using UnityEngine.InputSystem;

public class PatchWellState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[PatchWellState]</color> Entered: Tap the well to patch it.");
    }

    public void Tick()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            float distToPlane = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, distToPlane));

            EventBus.RaiseWellClicked(worldPos);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[PatchWellState]</color> Exited.");
    }
}