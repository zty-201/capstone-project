using System.Collections.Generic;
using UnityEngine;

// Undo/redo for the bridge builder's free-placement editing (see BridgeBuilderSystem.Execute/
// Undo/Redo). Plain data classes, not MonoBehaviours — each action captures just enough state
// to reverse and reapply itself, calling back into a handful of internal helpers on
// BridgeBuilderSystem rather than duplicating placement/removal logic. Node index is always
// captured/reused verbatim (never reallocated) so a later action's captured index can't go
// stale across an undo/redo cycle.
public interface IBridgeAction
{
    void Undo();
    void Redo();
}

// Placing one beam, which may also have created one or both of its endpoint nodes (a drag that
// started or ended on empty grid space rather than an already-existing point — see
// BridgeBuilderSystem.HandleDragEnd). Redo doubles as the initial "do": BridgeBuilderSystem.Execute
// calls Redo() to perform the placement the first time, exactly as it does on every subsequent redo.
public class PlaceBeamAction : IBridgeAction
{
    private readonly BridgeBuilderSystem system;
    private readonly BridgeNode existingA;
    private readonly Vector3 pointA;
    private readonly BridgeNode existingB;
    private readonly Vector3 pointB;
    private readonly BridgeMaterialData material;

    // -1 until this action creates that endpoint for the first time; once set, reused verbatim
    // on every later Redo so the recreated node always gets the same index back.
    private int createdAIndex = -1;
    private int createdBIndex = -1;

    private BridgeNode nodeA;
    private BridgeNode nodeB;
    private BridgePlank plank;

    public PlaceBeamAction(BridgeBuilderSystem system, BridgeNode existingA, Vector3 pointA,
        BridgeNode existingB, Vector3 pointB, BridgeMaterialData material)
    {
        this.system = system;
        this.existingA = existingA;
        this.pointA = pointA;
        this.existingB = existingB;
        this.pointB = pointB;
        this.material = material;
    }

    public void Redo()
    {
        nodeA = ResolveEndpoint(existingA, pointA, ref createdAIndex);
        nodeB = ResolveEndpoint(existingB, pointB, ref createdBIndex);
        plank = system.PlacePlankInternal(nodeA, nodeB, material);
    }

    private BridgeNode ResolveEndpoint(BridgeNode existing, Vector3 point, ref int createdIndex)
    {
        if (existing != null) return existing;

        int index = createdIndex >= 0 ? createdIndex : system.AllocateNodeIndex();
        createdIndex = index;
        return system.CreateNode(point, index);
    }

    public void Undo()
    {
        system.RemovePlankInternal(plank);
        if (createdBIndex >= 0) system.DestroyNodeInternal(nodeB);
        if (createdAIndex >= 0) system.DestroyNodeInternal(nodeA);
    }
}

// Deleting one non-anchor node and cascading the removal to every plank touching it (see
// BridgeBuilderSystem.TryDeleteNode). Captures each removed plank's other endpoint + material at
// construction time so Undo can recreate the whole star of connections exactly.
public class DeleteNodeAction : IBridgeAction
{
    private readonly BridgeBuilderSystem system;
    private readonly int nodeIndex;
    private readonly Vector3 nodePosition;
    private readonly List<(int otherIndex, BridgeMaterialData material)> connections = new();

    public DeleteNodeAction(BridgeBuilderSystem system, BridgeNode node)
    {
        this.system = system;
        nodeIndex = node.NodeIndex;
        nodePosition = node.transform.position;

        foreach (BridgePlank plank in system.FindPlanksTouching(node))
        {
            BridgeNode other = plank.NodeA == node ? plank.NodeB : plank.NodeA;
            connections.Add((other.NodeIndex, plank.Material));
        }
    }

    public void Redo()
    {
        BridgeNode node = system.FindNodeByIndex(nodeIndex);
        foreach (BridgePlank plank in system.FindPlanksTouching(node))
            system.RemovePlankInternal(plank);
        system.DestroyNodeInternal(node);
    }

    public void Undo()
    {
        BridgeNode node = system.CreateNode(nodePosition, nodeIndex);
        foreach (var (otherIndex, material) in connections)
        {
            BridgeNode other = system.FindNodeByIndex(otherIndex);
            system.PlacePlankInternal(node, other, material);
        }
    }
}
