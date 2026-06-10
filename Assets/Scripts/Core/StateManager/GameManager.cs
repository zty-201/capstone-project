using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameStateManager StateManager { get; private set; }
    public GameObject puzzleContainer;

    // Define your concrete states here so they can be reused without allocating memory every time
    public ExplorationState ExploreState { get; private set; }
    public PuzzleState PuzzleState { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize the architecture
        StateManager = new GameStateManager();
        ExploreState = new ExplorationState();
        PuzzleState = new PuzzleState();
    }

    private void Start()
    {
        // Boot up the game directly into Exploration mode
        StateManager.ChangeState(ExploreState);
    }

    private void Update()
    {
        // Feed the Unity Update loop into your pure C# state machine
        StateManager.Update();
    }
}