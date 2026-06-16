using System.Collections.Generic;
using UnityEngine;

public class GameStateManager
{
    private Dictionary<GameStateType, IState> _states = new Dictionary<GameStateType, IState>();

    public IState CurrentState { get; private set; }
    public GameStateType CurrentStateType { get; private set; }

    public void RegisterStates(Dictionary<GameStateType, IState> states)
    {
        _states = states;
    }

    public void ChangeState(GameStateType type)
    {
        if (!_states.TryGetValue(type, out IState newState))
        {
            Debug.LogError($"[GameStateManager] No state registered for {type}");
            return;
        }

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentStateType = type;
        CurrentState.Enter();

        Debug.Log($"<color=cyan>[GameStateManager]</color> Transitioned to: {newState.GetType().Name}");
    }

    public void Update()
    {
        CurrentState?.Tick();
    }
}
