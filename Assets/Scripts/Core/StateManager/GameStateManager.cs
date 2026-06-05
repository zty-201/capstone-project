using UnityEngine;

public class GameManager : MonoBehaviour
{
    // The singleton instance so other scripts can request state changes
    public static GameManager Instance { get; private set; }

    private IState currentState;

    private void Awake()
    {
        // Basic Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Persists across scenes if needed
    }

    private void Start()
    {
        // Fire the initialization event you created earlier
        EventBus.OnGameInitialized?.Invoke();

        // TODO: Initialize your starting state here (e.g., MainMenuState or ExplorationState)
        ChangeState(new ExplorationState());
    }

    private void Update()
    {
        // Run the logic for whatever state is currently active
        currentState?.Execute();
    }

    // This is the core method you will call to switch between Exploration, Minigames, etc.
    public void ChangeState(IState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;

        if (currentState != null)
        {
            currentState.Enter();
            Debug.Log($"<color=green>[GameManager]</color> Changed state to: {currentState.GetType().Name}");
        }
    }
}