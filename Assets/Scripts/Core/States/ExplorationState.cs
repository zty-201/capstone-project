using UnityEngine;
using UnityEngine.EventSystems;

public class ExplorationState : IState
{
    public void Enter()
    {
        Debug.Log("<color=green>[State]</color> Exploration Mode Active. Movement Unlocked.");
    }

    public void Tick()
    {
        // A press on a HUD element (Mission Directory, Settings...) is that element's click, not a
        // walk/interact order — same guard as BridgeBuilderState.
        if (PointerInput.TryGetPrimaryPressWorldPosition(out Vector3 worldPos)
            && !EventSystem.current.IsPointerOverGameObject())
            EventBus.RaiseMapClicked(worldPos);
    }

    public void Exit()
    {
        Debug.Log("<color=green>[State]</color> Exiting Exploration Mode.");
    }
}
