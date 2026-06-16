using System.Collections.Generic;

public class NodeMinHeap
{
    private readonly List<GridNode> _data = new List<GridNode>();

    public int Count => _data.Count;

    public void Enqueue(GridNode node)
    {
        _data.Add(node);
        BubbleUp(_data.Count - 1);
    }

    public GridNode Dequeue()
    {
        GridNode top = _data[0];
        int last = _data.Count - 1;
        _data[0] = _data[last];
        _data.RemoveAt(last);
        if (_data.Count > 0) SiftDown(0);
        return top;
    }

    private void BubbleUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (_data[parent].fCost <= _data[i].fCost) break;
            Swap(i, parent);
            i = parent;
        }
    }

    private void SiftDown(int i)
    {
        int count = _data.Count;
        while (true)
        {
            int smallest = i;
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            if (left < count && _data[left].fCost < _data[smallest].fCost) smallest = left;
            if (right < count && _data[right].fCost < _data[smallest].fCost) smallest = right;
            if (smallest == i) break;
            Swap(i, smallest);
            i = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        GridNode tmp = _data[a];
        _data[a] = _data[b];
        _data[b] = tmp;
    }
}
