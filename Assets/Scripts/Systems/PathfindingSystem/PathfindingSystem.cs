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

    private void Awake()
    {
        if (collisionTilemap == null)
            Debug.LogError($"[{name}] collisionTilemap is not assigned!", this);
    }

    private void Start()
    {
        gridSystem = new GridSystem(gridWidth, gridHeight, cellSize, gridOrigin);

        if (collisionTilemap != null)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 worldPos = gridSystem.GetWorldPosition(x, y);
                    Vector3Int tilePosition = collisionTilemap.WorldToCell(worldPos);

                    if (collisionTilemap.HasTile(tilePosition))
                    {
                        GridNode node = gridSystem.GetNode(x, y);
                        if (node != null) node.isWalkable = false;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("<color=yellow>[Pathfinding]</color> Collision Tilemap is missing in the Inspector!");
        }
    }

    public void GetGridCoordinates(Vector3 worldPos, out int x, out int y)
    {
        gridSystem.GetXY(worldPos, out x, out y);
    }

    public Vector3 GetWorldPositionCenter(int x, int y)
    {
        return gridSystem.GetWorldPosition(x, y) + new Vector3(cellSize / 2f, cellSize / 2f, 0);
    }

    private void OnEnable() => EventBus.OnPathRequested += CalculatePath;
    private void OnDisable() => EventBus.OnPathRequested -= CalculatePath;

    private void CalculatePath(Vector2Int start, Vector2Int end)
    {
        GridNode startNode = gridSystem.GetNode(start.x, start.y);
        GridNode endNode = gridSystem.GetNode(end.x, end.y);

        if (startNode == null || endNode == null || !endNode.isWalkable) return;

        // Reset all nodes
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                GridNode node = gridSystem.GetNode(x, y);
                node.gCost = int.MaxValue;
                node.cameFromNode = null;
            }

        startNode.gCost = 0;
        startNode.hCost = CalculateManhattanDistance(startNode, endNode);

        NodeMinHeap openHeap = new NodeMinHeap();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();
        openHeap.Enqueue(startNode);

        while (openHeap.Count > 0)
        {
            GridNode currentNode = openHeap.Dequeue();

            // Skip stale heap entries for nodes already finalized
            if (closedSet.Contains(currentNode)) continue;
            closedSet.Add(currentNode);

            if (currentNode == endNode)
            {
                EventBus.RaisePathGenerated(BuildPath(endNode));
                return;
            }

            foreach (GridNode neighbor in GetNeighbors(currentNode))
            {
                if (closedSet.Contains(neighbor) || !neighbor.isWalkable) continue;

                int tentativeGCost = currentNode.gCost + CalculateManhattanDistance(currentNode, neighbor);

                if (tentativeGCost < neighbor.gCost)
                {
                    neighbor.cameFromNode = currentNode;
                    neighbor.gCost = tentativeGCost;
                    neighbor.hCost = CalculateManhattanDistance(neighbor, endNode);
                    openHeap.Enqueue(neighbor);
                }
            }
        }

        Debug.LogWarning("No valid path found.");
    }

    private List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();
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

    private List<GridNode> BuildPath(GridNode endNode)
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

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || gridSystem == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
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
