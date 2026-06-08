using UnityEngine;

public class GameStateManager
{
    public IState CurrentState { get; private set; }

    public void ChangeState(IState newState)
    {
        // Clean up the old state before entering the new one
        if (CurrentState != null)
        {
            CurrentState.Exit();
        }

        CurrentState = newState;
        CurrentState.Enter();

        Debug.Log($"<color=cyan>[GameStateManager]</color> Transitioned to: {newState.GetType().Name}");
    }

    public void Update()
    {
        // Pass the frame tick down to whichever state is currently active
        if (CurrentState != null)
        {
            CurrentState.Tick();
        }
    }
}