using System.Collections.Generic;
using UnityEngine;

public class PipePuzzleSystem : MonoBehaviour
{
    [Header("Puzzle Dimensions")]
    public int width = 3;
    public int height = 3;

    private PipeNode[,] grid;

    [Header("Win Conditions")]
    public Vector2Int startPos = new Vector2Int(0, 0); // Bottom-Left (The Pump)
    public Vector2Int endPos = new Vector2Int(2, 2);   // Top-Right (The Crops)

    // Creates a basic hardcoded layout for testing the MVP backend
    private void Start()
    {
        InitializeGridFromScene();
    }

    private void InitializeGridFromScene()
    {
        // 1. Create the empty mathematical grid
        grid = new PipeNode[width, height];

        // 2. Find every visual pipe you placed in the Unity Scene
        PipeVisual[] visualPipes = FindObjectsByType<PipeVisual>(FindObjectsInactive.Exclude);

        foreach (PipeVisual pipe in visualPipes)
        {
            // 3. Ask the visual pipe what its starting math should be based on its Z-rotation
            PipeDirection startingBits = pipe.GetStartingBits();

            // 4. Inject it into the mathematical grid
            grid[pipe.gridX, pipe.gridY] = new PipeNode(pipe.gridX, pipe.gridY, startingBits);
        }
    }

    // The single entry point for input. Call this when the player clicks a pipe.
    public void RotatePipeAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        // THE NEW GUARD CLAUSE: If this grid space is empty, ignore the click!
        if (grid[x, y] == null) return;

        grid[x, y].RotateClockwise();

        if (CheckWaterFlow())
        {
            Debug.Log("<color=cyan>[PipePuzzleSystem]</color> Connection established!");
        }
    }

    // --- The Graph Traversal Backend ---

    private bool CheckWaterFlow()
    {
        // Reset the power state of all pipes before calculating the new flow
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y].isPowered = false;

        Stack<PipeNode> stack = new Stack<PipeNode>();
        HashSet<PipeNode> visited = new HashSet<PipeNode>();

        // Begin the flow at the water pump
        PipeNode startNode = grid[startPos.x, startPos.y];
        stack.Push(startNode);

        while (stack.Count > 0)
        {
            PipeNode current = stack.Pop();

            if (visited.Contains(current)) continue;

            visited.Add(current);
            current.isPowered = true;

            // Success Condition: The flow has reached the exact coordinates of the crops
            if (current.x == endPos.x && current.y == endPos.y)
            {
                return true;
            }

            // Attempt to push flow to all 4 cardinal neighbors
            EvaluateNeighbor(current, PipeDirection.Up, 0, 1, PipeDirection.Down, stack);
            EvaluateNeighbor(current, PipeDirection.Right, 1, 0, PipeDirection.Left, stack);
            EvaluateNeighbor(current, PipeDirection.Down, 0, -1, PipeDirection.Up, stack);
            EvaluateNeighbor(current, PipeDirection.Left, -1, 0, PipeDirection.Right, stack);
        }

        // If the stack empties and we never hit the end node, the pipes are disconnected
        return false;
    }

    // --- The Core Bitwise Verification ---

    private void EvaluateNeighbor(PipeNode current, PipeDirection dirToNeighbor, int offsetX, int offsetY, PipeDirection requiredNeighborDir, Stack<PipeNode> stack)
    {
        // 1. Bitwise AND: Does the CURRENT pipe even have an opening pointing in this direction?
        // (If the result is 0, the bit is missing. No flow possible.)
        if ((current.currentConnections & dirToNeighbor) == 0) return;

        int nx = current.x + offsetX;
        int ny = current.y + offsetY;

        // 2. Array Bounds: Does the neighbor exist on the grid?
        if (nx < 0 || nx >= width || ny < 0 || ny >= height) return;

        PipeNode neighbor = grid[nx, ny];

        // 3. Bitwise AND Inverse: Does the NEIGHBOR pipe have an opening pointing BACK at us?
        if ((neighbor.currentConnections & requiredNeighborDir) != 0)
        {
            // Valid connection! Push it to the stack so water can continue flowing from it.
            stack.Push(neighbor);
        }
    }
}