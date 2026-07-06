using UnityEngine;

public class ExplorationState : IState
{
    public void Enter()
    {
        Debug.Log("<color=green>[State]</color> Exploration Mode Active. Movement Unlocked.");
    }

    public void Tick()
    {
        if (PointerInput.TryGetPrimaryPressWorldPosition(out Vector3 worldPos))
            EventBus.RaiseMapClicked(worldPos);
    }

    public void Exit()
    {
        Debug.Log("<color=green>[State]</color> Exiting Exploration Mode.");
    }
}