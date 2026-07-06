using UnityEngine;
using UnityEngine.InputSystem;

public class PlanningState : IState
{
    public void Enter()
    {
        Debug.Log("<color=magenta>[PlanningState]</color> Entered: Awaiting solution choice.");
    }

    public void Tick()
    {
        if (PointerInput.PrimaryPressedThisFrame())
            PlanningUI.Instance.OnAdvance();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            PlanningUI.Instance.Hide();
            GameManager.Instance.StateManager.ChangeState(GameStateType.Exploration);
        }
    }

    public void Exit()
    {
        Debug.Log("<color=magenta>[PlanningState]</color> Exited.");
    }
}
