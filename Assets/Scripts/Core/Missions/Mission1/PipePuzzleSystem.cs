using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipePuzzleSystem : MonoBehaviour
{
    public static PipePuzzleSystem Instance { get; private set; }

    [Header("Puzzle Dimensions")]
    public int width = 3;
    public int height = 3;
    public int missionID = 1;

    private PipeNode[,] grid;
    private PipeVisual[] visuals;
    private PipeDirection[,] originalBits;
    private float[,] originalRotationZ;

    [Header("Win Conditions")]
    public Vector2Int startPos = new Vector2Int(0, 0);
    public Vector2Int endPos = new Vector2Int(2, 2);

    // OnMissionsNeedReview is subscribed here (not OnEnable/OnDisable): if this ever ends up
    // living inside a container that gets disabled after this path's mission completes, an
    // OnEnable/OnDisable subscription would already be gone by the time a review request —
    // which can only happen after the mission is complete — needs to reach it.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    private void Start()
    {
        InitializeGridFromScene();
    }

    private void InitializeGridFromScene()
    {
        grid = new PipeNode[width, height];
        originalBits = new PipeDirection[width, height];
        originalRotationZ = new float[width, height];

        // Include inactive pipes too: this mission may be completed via the trivial (well) path
        // instead, in which case the optimal-path container holding these pipes never activates,
        // but a later stage reset still needs their original state cached.
        visuals = FindObjectsByType<PipeVisual>(FindObjectsInactive.Include);

        foreach (PipeVisual pipe in visuals)
        {
            PipeDirection startingBits = pipe.GetStartingBits();
            originalBits[pipe.gridX, pipe.gridY] = startingBits;
            originalRotationZ[pipe.gridX, pipe.gridY] = pipe.transform.eulerAngles.z;
            grid[pipe.gridX, pipe.gridY] = new PipeNode(pipe.gridX, pipe.gridY, startingBits);
        }
    }

    public void ResetPuzzle()
    {
        if (visuals == null) return;

        foreach (PipeVisual pipe in visuals)
        {
            pipe.ResetRotation(originalRotationZ[pipe.gridX, pipe.gridY]);
            grid[pipe.gridX, pipe.gridY] = new PipeNode(pipe.gridX, pipe.gridY, originalBits[pipe.gridX, pipe.gridY]);
        }

        UpdateVisuals();
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        ResetPuzzle();
    }

    public void RotatePipeAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (grid[x, y] == null) return;

        grid[x, y].RotateClockwise();

        bool solved = CheckWaterFlow();
        UpdateVisuals();
        if (solved) StartCoroutine(HandlePuzzleVictory());
    }

    private bool CheckWaterFlow()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] != null) grid[x, y].isPowered = false;

        Stack<PipeNode> stack = new Stack<PipeNode>();
        HashSet<PipeNode> visited = new HashSet<PipeNode>();

        PipeNode startNode = grid[startPos.x, startPos.y];
        stack.Push(startNode);

        while (stack.Count > 0)
        {
            PipeNode current = stack.Pop();

            if (visited.Contains(current)) continue;
            visited.Add(current);
            current.isPowered = true;

            if (current.x == endPos.x && current.y == endPos.y)
                return true;

            EvaluateNeighbor(current, PipeDirection.Up,    0,  1, PipeDirection.Down,  stack);
            EvaluateNeighbor(current, PipeDirection.Right, 1,  0, PipeDirection.Left,  stack);
            EvaluateNeighbor(current, PipeDirection.Down,  0, -1, PipeDirection.Up,    stack);
            EvaluateNeighbor(current, PipeDirection.Left, -1,  0, PipeDirection.Right, stack);
        }

        return false;
    }

    private void EvaluateNeighbor(PipeNode current, PipeDirection dirToNeighbor, int offsetX, int offsetY, PipeDirection requiredNeighborDir, Stack<PipeNode> stack)
    {
        if ((current.currentConnections & dirToNeighbor) == 0) return;

        int nx = current.x + offsetX;
        int ny = current.y + offsetY;

        if (nx < 0 || nx >= width || ny < 0 || ny >= height) return;

        PipeNode neighbor = grid[nx, ny];
        if (neighbor == null) return;

        if ((neighbor.currentConnections & requiredNeighborDir) != 0)
            stack.Push(neighbor);
    }

    private void UpdateVisuals()
    {
        foreach (PipeVisual v in visuals)
        {
            PipeNode node = grid[v.gridX, v.gridY];
            if (node != null) v.SetPowered(node.isPowered);
        }
    }

    private IEnumerator HandlePuzzleVictory()
    {
        Debug.Log("<color=cyan>[PipePuzzleSystem]</color> Puzzle Solved!");
        yield return new WaitForSeconds(1.5f);

        EventBus.RaiseMissionCompleted(missionID, true);
    }
}
