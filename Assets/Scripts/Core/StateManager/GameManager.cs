using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameStateManager StateManager { get; private set; }
    public GameObject puzzleContainer;
    public MissionBoardState MissionBoardState { get; private set; }
    public ExplorationState ExploreState { get; private set; }
    public DialogueState DialogueState { get; private set; }
    public PuzzleState PuzzleState { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        StateManager = new GameStateManager();
        ExploreState = new ExplorationState();
        DialogueState = new DialogueState();
        PuzzleState = new PuzzleState();
        MissionBoardState = new MissionBoardState();
    }

    private void Start()
    {
        StateManager.ChangeState(ExploreState);
    }

    private void Update()
    {
        StateManager.Update();
    }
}