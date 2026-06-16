using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipePuzzleSystem : MonoBehaviour
{
    [Header("Puzzle Dimensions")]
    public int width = 3;
    public int height = 3;
    public int missionID = 1;

    private PipeNode[,] grid;

    [Header("Win Conditions")]
    public Vector2Int startPos = new Vector2Int(0, 0);
    public Vector2Int endPos = new Vector2Int(2, 2);

    private void Start()
    {
        InitializeGridFromScene();
    }

    private void InitializeGridFromScene()
    {
        grid = new PipeNode[width, height];

        PipeVisual[] visualPipes = FindObjectsByType<PipeVisual>(FindObjectsInactive.Exclude);

        foreach (PipeVisual pipe in visualPipes)
        {
            PipeDirection startingBits = pipe.GetStartingBits();
            grid[pipe.gridX, pipe.gridY] = new PipeNode(pipe.gridX, pipe.gridY, startingBits);
        }
    }

    public void RotatePipeAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        if (grid[x, y] == null) return;

        grid[x, y].RotateClockwise();

        if (CheckWaterFlow())
        {
            StartCoroutine(HandlePuzzleVictory());
        }
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

    private IEnumerator HandlePuzzleVictory()
    {
        Debug.Log("<color=cyan>[PipePuzzleSystem]</color> Puzzle Solved!");
        yield return new WaitForSeconds(1.5f);

        EventBus.RaiseMissionCompleted(missionID, true);
    }
}
