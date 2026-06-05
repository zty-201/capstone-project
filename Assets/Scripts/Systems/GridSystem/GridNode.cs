public class GridNode
{
    public int x;
    public int y;
    public bool isWalkable;

    // A* Pathfinding Variables
    public int gCost;
    public int hCost;
    public int fCost { get { return gCost + hCost; } }
    public GridNode cameFromNode;

    public GridNode(int gridX, int gridY, bool walkable)
    {
        x = gridX;
        y = gridY;
        isWalkable = walkable;
    }
}