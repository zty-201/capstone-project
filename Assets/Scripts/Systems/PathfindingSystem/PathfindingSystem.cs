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

    public void SetWalkable(Vector3 worldPos, bool walkable)
    {
        gridSystem.GetXY(worldPos, out int x, out int y);
        GridNode node = gridSystem.GetNode(x, y);
        if (node != null) node.isWalkable = walkable;
    }

    public Vector3 GetWorldPositionCenter(int x, int y)
    {
        return gridSystem.GetWorldPosition(x, y) + new Vector3(cellSize / 2f, cellSize / 2f, 0);
    }

    private void OnEnable() => EventBus.OnPathRequested += CalculatePath;
    private void OnDisable() => EventBus.OnPathRequested -= CalculatePath;

    private void CalculatePath(Vector2Int start, Vector2Int end)
    {
        List<GridNode> path = RequestPathSync(start, end);
        if (path != null) EventBus.RaisePathGenerated(path);
    }

    // Synchronous path query for callers that don't want to go through the shared
    // OnPathRequested/OnPathGenerated broadcast (e.g. NPCs), since every subscriber
    // receives every OnPathGenerated event regardless of who asked for it.
    public List<GridNode> RequestPathSync(Vector2Int start, Vector2Int end)
    {
        GridNode startNode = gridSystem.GetNode(start.x, start.y);
        GridNode endNode = gridSystem.GetNode(end.x, end.y);

        if (startNode == null || endNode == null || !endNode.isWalkable) return null;

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
                return BuildPath(endNode);

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
        return null;
    }

    // Picks a random walkable cell reachable from `from` via flood-fill, so NPC patrol
    // logic never targets a walkable "island" cut off by unwalkable tiles.
    public Vector2Int GetRandomWalkableCoordinates(Vector2Int from)
    {
        List<GridNode> reachable = new List<GridNode>(GetReachableNodes(from));
        GridNode picked = reachable[Random.Range(0, reachable.Count)];
        return new Vector2Int(picked.x, picked.y);
    }

    // Resolves a click on a cell `from` can't reach (unwalkable, off-grid, or a walkable pocket
    // boxed in by walls) to the first cell on the straight line from `target` back to `from` that
    // `from` can reach — the near side of whatever was clicked. Checking reachability rather than
    // just walkability is what keeps a boxed-in pocket from being picked and then failing A*,
    // leaving the player standing still. `from` itself is always reachable, so the walk always
    // ends on some cell.
    public Vector2Int GetNearestReachableTowards(Vector2Int from, Vector2Int target)
    {
        HashSet<GridNode> reachable = GetReachableNodes(from);
        int steps = Mathf.Max(Mathf.Abs(from.x - target.x), Mathf.Abs(from.y - target.y));
        if (steps == 0) return from;
        for (int i = 0; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(target, from, (float)i / steps);
            GridNode node = gridSystem.GetNode(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
            if (node != null && reachable.Contains(node)) return new Vector2Int(node.x, node.y);
        }
        return from;
    }

    // Flood-fill over walkable neighbors; `from` itself is always included.
    private HashSet<GridNode> GetReachableNodes(Vector2Int from)
    {
        GridNode startNode = gridSystem.GetNode(from.x, from.y);

        HashSet<GridNode> visited = new HashSet<GridNode> { startNode };
        Queue<GridNode> frontier = new Queue<GridNode>();
        frontier.Enqueue(startNode);

        while (frontier.Count > 0)
        {
            GridNode current = frontier.Dequeue();

            foreach (GridNode neighbor in GetNeighbors(current))
            {
                if (!neighbor.isWalkable || visited.Contains(neighbor)) continue;
                visited.Add(neighbor);
                frontier.Enqueue(neighbor);
            }
        }

        return visited;
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
