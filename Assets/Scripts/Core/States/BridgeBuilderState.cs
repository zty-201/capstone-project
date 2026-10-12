using UnityEngine;
using UnityEngine.EventSystems;
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
            BridgeBuilderSystem.Instance.ShowBridgeLayers();
        }
    }

    public void Tick()
    {
        BridgeBuilderSystem system = BridgeBuilderSystem.Instance;

        if (system != null && PointerInput.TryGetPrimaryWorldPosition(out Vector3 worldPos))
        {
            // A press on a HUD button (Undo/Delete/a material...) is that button's click, not a
            // build gesture — without this guard it also deselected the node Delete was about to
            // act on. Only the press is filtered: HandleDragUpdate/End no-op without a started drag.
            if (PointerInput.PrimaryPressedThisFrame())
            {
                if (!EventSystem.current.IsPointerOverGameObject()) system.HandleDragStart(worldPos);
            }
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

        // No ESC exit: the only way into this state is MinigameActivator on OnSolutionSelected,
        // and BridgeInteractable has already disabled itself by then, so leaving would strand
        // the player with no way back into the build.
    }

    public void Exit()
    {
        Debug.Log("<color=orange>[BridgeBuilderState]</color> Exited: Returning to town.");

        if (BridgeBuilderSystem.Instance != null)
        {
            BridgeBuilderSystem.Instance.CancelDrag();
            BridgeBuilderSystem.Instance.HideBridgeLayers();
            BridgeBuilderSystem.Instance.BridgeViewCamera.SetActive(false);
            BridgeBuilderSystem.Instance.PlayerCamera.SetActive(true);
        }
    }
}
