using UnityEngine;

public class GridSystem
{
    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private GridNode[,] gridArray;

    // Constructor to build the mathematical grid
    // 1. Update the Constructor to require the origin
    public GridSystem(int width, int height, float cellSize, Vector3 originPosition)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new GridNode[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                gridArray[x, y] = new GridNode(x, y, true);
            }
        }
    }

    // 2. Add Helper: Convert Array Index to World Space
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(x, y) * cellSize + originPosition;
    }

    // 3. Add Helper: Convert World Space to Array Index
    public void GetXY(Vector3 worldPosition, out int x, out int y)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / cellSize);
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