public class GridNode
{
    public int x;
    public int y;
    public bool isWalkable;

    public GridNode(int gridX, int gridY, bool walkable)
    {
        x = gridX;
        y = gridY;
        isWalkable = walkable;
    }
}