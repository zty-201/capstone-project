using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathfindingSystem : MonoBehaviour
{
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public Vector3 gridOrigin;

    [Header("Visual Bridges")]
    public Tilemap collisionTilemap;

    private GridSystem gridSystem;
    private void Start()
    {
        // 1. Initialize the mathematical grid
        gridSystem = new GridSystem(gridWidth, gridHeight, cellSize, gridOrigin);

        // 2. Scan the visual Tilemap to update the math
        if (collisionTilemap != null)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // Convert our mathematical (X, Y) to Unity's Tilemap (X, Y, Z) coordinate
                    Vector3Int tilePosition = new Vector3Int(x, y, 0);

                    // If a tile is painted on the collision layer at this coordinate...
                    if (collisionTilemap.HasTile(tilePosition))
                    {
                        // ...flag our invisible backend node as unwalkable
                        GridNode node = gridSystem.GetNode(x, y);
                        if (node != null)
                        {
                            node.isWalkable = false;
                        }
                    }
                }
            }
        }
    }

    public void GetGridCoordinates(Vector3 worldPos, out int x, out int y)
    {
        gridSystem.GetXY(worldPos, out x, out y);
    }

    public Vector3 GetWorldPositionCenter(int x, int y)
    {
        // Add half a cell size to the origin-adjusted position to get the dead center
        return gridSystem.GetWorldPosition(x, y) + new Vector3(cellSize / 2f, cellSize / 2f, 0);
    }

    private void OnEnable() => EventBus.OnPathRequested += CalculatePath;
    private void OnDisable() => EventBus.OnPathRequested -= CalculatePath;

    private void CalculatePath(Vector2Int start, Vector2Int end)
    {
        GridNode startNode = gridSystem.GetNode(start.x, start.y);
        GridNode endNode = gridSystem.GetNode(end.x, end.y);

        if (startNode == null || endNode == null || !endNode.isWalkable) return;

        List<GridNode> openList = new List<GridNode> { startNode };
        HashSet<GridNode> closedList = new HashSet<GridNode>();

        // Reset all nodes
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridNode node = gridSystem.GetNode(x, y);
                node.gCost = int.MaxValue;
                node.cameFromNode = null;
            }
        }

        startNode.gCost = 0;
        startNode.hCost = CalculateManhattanDistance(startNode, endNode);

        while (openList.Count > 0)
        {
            GridNode currentNode = GetLowestFCostNode(openList);

            if (currentNode == endNode)
            {
                // Path found! Broadcast it.
                EventBus.OnPathGenerated?.Invoke(CalculatePath(endNode));
                return;
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (GridNode neighbor in GetNeighbors(currentNode))
            {
                if (closedList.Contains(neighbor) || !neighbor.isWalkable) continue;

                int tentativeGCost = currentNode.gCost + CalculateManhattanDistance(currentNode, neighbor);

                if (tentativeGCost < neighbor.gCost)
                {
                    neighbor.cameFromNode = currentNode;
                    neighbor.gCost = tentativeGCost;
                    neighbor.hCost = CalculateManhattanDistance(neighbor, endNode);

                    if (!openList.Contains(neighbor)) openList.Add(neighbor);
                }
            }
        }

        Debug.LogWarning("No valid path found.");
    }

    private List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();
        // 4-way orthogonal movement
        if (node.x > 0) neighbors.Add(gridSystem.GetNode(node.x - 1, node.y));
        if (node.x < gridWidth - 1) neighbors.Add(gridSystem.GetNode(node.x + 1, node.y));
        if (node.y > 0) neighbors.Add(gridSystem.GetNode(node.x, node.y - 1));
        if (node.y < gridHeight - 1) neighbors.Add(gridSystem.GetNode(node.x, node.y + 1));
        return neighbors;
    }

    private int CalculateManhattanDistance(GridNode a, GridNode b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private GridNode GetLowestFCostNode(List<GridNode> nodeList)
    {
        GridNode lowest = nodeList[0];
        for (int i = 1; i < nodeList.Count; i++)
        {
            if (nodeList[i].fCost < lowest.fCost) lowest = nodeList[i];
        }
        return lowest;
    }

    private List<GridNode> CalculatePath(GridNode endNode)
    {
        List<GridNode> path = new List<GridNode> { endNode };
        GridNode currentNode = endNode;
        while (currentNode.cameFromNode != null)
        {
            path.Add(currentNode.cameFromNode);
            currentNode = currentNode.cameFromNode;
        }
        path.Reverse();
        return path;
    }

    // ---------------------------------------------------------
    // DEBUG VISUALS: Draw the grid in the Scene/Game view
    // ---------------------------------------------------------
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || gridSystem == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Use the helper method instead of raw math!
                Vector3 cellPosition = gridSystem.GetWorldPosition(x, y);
                Vector3 centerOffset = new Vector3(cellSize / 2f, cellSize / 2f, 0);

                GridNode node = gridSystem.GetNode(x, y);

                if (node != null && !node.isWalkable)
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
                    Gizmos.DrawCube(cellPosition + centerOffset, new Vector3(cellSize, cellSize, 0));
                }
                else
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(cellPosition + centerOffset, new Vector3(cellSize, cellSize, 0));
                }
            }
        }
    }
}