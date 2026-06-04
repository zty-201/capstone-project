using UnityEngine;

public class GridSystem
{
    private int width;
    private int height;
    private float cellSize;
    private GridNode[,] gridArray;

    // Constructor to build the mathematical grid
    public GridSystem(int width, int height, float cellSize)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;

        gridArray = new GridNode[width, height];

        // Loop through and initialize every node
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // By default, we will say everything is walkable. 
                // Later, we will map this to Unity's collision/tilemap data.
                gridArray[x, y] = new GridNode(x, y, true);
            }
        }
    }

    /// <summary>
    /// This is the core method Daryl's touch system will call to see if a move is legal.
    /// </summary>
    public bool IsValidMove(int targetX, int targetY)
    {
        // 1. Check if the target is out of bounds
        if (targetX < 0 || targetY < 0 || targetX >= width || targetY >= height)
        {
            return false;
        }

        // 2. Check if the tile is blocked (e.g., by a building or rubbish)
        return gridArray[targetX, targetY].isWalkable;
    }

    // Helper to get a specific node if needed
    public GridNode GetNode(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < width && y < height)
        {
            return gridArray[x, y];
        }
        return null;
    }
}