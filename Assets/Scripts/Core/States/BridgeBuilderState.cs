using UnityEngine;
using UnityEngine.InputSystem;

public class BridgeBuilderState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[BridgeBuilderState]</color> Entered: Building the bridge.");

        // The physics playground never moves (see BridgeBuilderSystem's class comment for why) —
        // instead, swap to a second camera dedicated to framing it, and hand tracking back to the
        // player camera on Exit below.
        if (BridgeBuilderSystem.Instance != null)
        {
            BridgeBuilderSystem.Instance.PlayerCamera.SetActive(false);
            BridgeBuilderSystem.Instance.BridgeViewCamera.SetActive(true);
            BridgeBuilderSystem.Instance.ShowBridgeLayer();
        }
    }

    public void Tick()
    {
        BridgeBuilderSystem system = BridgeBuilderSystem.Instance;

        if (system != null && PointerInput.TryGetPrimaryWorldPosition(out Vector3 worldPos))
        {
            if (PointerInput.PrimaryPressedThisFrame()) system.HandleDragStart(worldPos);
            else if (PointerInput.PrimaryHeld()) system.HandleDragUpdate(worldPos);
            else if (PointerInput.PrimaryReleasedThisFrame()) system.HandleDragEnd(worldPos);
        }

        if (system != null && Keyboard.current != null)
        {
            if (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame)
                system.DeleteSelectedNode();

            bool ctrl = Keyboard.current.ctrlKey.isPressed;
            if (ctrl && Keyboard.current.zKey.wasPressedThisFrame) system.Undo();
            else if (ctrl && Keyboard.current.yKey.wasPressedThisFrame) system.Redo();
        }

        // ESC is only honored mid-Building: while a physics test is running, leaving to
        // Exploration would strand the test's pass/fail resolution off-screen (physics keeps
        // simulating regardless of GameStateType) with no way back into this state to see it.
        bool canLeave = system == null || system.Phase == BridgeBuilderSystem.BuildPhase.Building;

        if (canLeave && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[BridgeBuilderState]</color> ESC pressed. Transitioning back to Exploration State...");
            GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[BridgeBuilderState]</color> Exited: Returning to town.");

        if (BridgeBuilderSystem.Instance != null)
        {
            BridgeBuilderSystem.Instance.CancelDrag();
            BridgeBuilderSystem.Instance.HideBridgeLayer();
            BridgeBuilderSystem.Instance.BridgeViewCamera.SetActive(false);
            BridgeBuilderSystem.Instance.PlayerCamera.SetActive(true);
        }
    }
}
