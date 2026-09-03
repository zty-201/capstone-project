using UnityEngine;
using UnityEngine.InputSystem;

public class BridgeBuilderState : IState
{
    public void Enter()
    {
        Debug.Log("<color=orange>[BridgeBuilderState]</color> Entered: Building the bridge.");
    }

    public void Tick()
    {
        if (PointerInput.TryGetPrimaryPressWorldPosition(out Vector3 worldPos))
            EventBus.RaiseBridgeClicked(worldPos);

        // ESC is only honored mid-Building: while a physics test is running, leaving to
        // Exploration would strand the test's pass/fail resolution off-screen (physics keeps
        // simulating regardless of GameStateType) with no way back into this state to see it.
        bool canLeave = BridgeBuilderSystem.Instance == null
            || BridgeBuilderSystem.Instance.Phase == BridgeBuilderSystem.BuildPhase.Building;

        if (canLeave && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("<color=orange>[BridgeBuilderState]</color> ESC pressed. Transitioning back to Exploration State...");
            GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[BridgeBuilderState]</color> Exited: Returning to town.");
    }
}
