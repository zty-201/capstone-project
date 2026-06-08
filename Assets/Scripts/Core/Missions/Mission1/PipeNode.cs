public class PipeNode
{
    public int x;
    public int y;
    public PipeDirection currentConnections;
    public bool isPowered; // True if water is flowing through it

    public PipeNode(int x, int y, PipeDirection initialConnections)
    {
        this.x = x;
        this.y = y;
        this.currentConnections = initialConnections;
        this.isPowered = false;
    }

    // The mathematical rotation
    public void RotateClockwise()
    {
        // 1. Cast the enum to an integer
        int bits = (int)currentConnections;

        // 2. Shift all bits to the left by 1 (Up becomes Right, Right becomes Down, etc.)
        // 3. Handle the wrap-around (Left bit 8 shifted becomes 16, we need it to wrap back to 1)
        int rotatedBits = (bits << 1) | (bits >> 3);

        // 4. Use bitwise AND with 15 (which is 1111 in binary) to strip away any overflow bits
        currentConnections = (PipeDirection)(rotatedBits & 15);
    }
}