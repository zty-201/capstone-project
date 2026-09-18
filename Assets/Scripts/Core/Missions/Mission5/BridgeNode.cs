using UnityEngine;

// A connection point in the bridge builder's playground (see BridgeBuilderSystem). Anchor nodes
// (isAnchor) are solid ground — a pure Transform, Editor-authored, referenced by a plank's
// HingeJoint2D as a fixed world-space point (Unity treats a joint's connectedAnchor as a world
// position whenever connectedBody is left null) — and never destroyed. Deck nodes are entirely
// player-placed at runtime (see BridgeBuilderSystem.CreateNode/InitializeRuntime below): they
// carry their own Rigidbody2D so every plank meeting at the same point moves together as one
// hinge, are Kinematic (locked in place, ignoring forces) while building and only switched to
// Dynamic for the physics test — same lifecycle as every BridgePlank, see
// BridgeBuilderSystem.StartTest — and are destroyed outright (not repositioned) whenever the
// bridge resets, since nothing about a deck node's placement is Editor-authored to return to.
//
// Node resolution (which node, if any, a press/drag point is near) is centralized in
// BridgeBuilderSystem rather than each node self-testing clicks — free placement needs "nearest
// point within radius, existing node or empty grid space," which no single node can answer about
// itself in isolation, so this class carries no collider/click-handling of its own.
public class BridgeNode : MonoBehaviour
{
    [SerializeField] private int nodeIndex;
    [SerializeField] private bool isAnchor;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    private Rigidbody2D rb;

    public int NodeIndex => nodeIndex;
    public bool IsAnchor => isAnchor;
    public Rigidbody2D Body => rb;

    private void Awake()
    {
        if (!isAnchor) rb = GetComponent<Rigidbody2D>();
    }

    // Called once, immediately after Instantiate, for every runtime-created deck node — see
    // BridgeBuilderSystem.CreateNode. isAnchor is left at the prefab's default (false).
    public void InitializeRuntime(int index)
    {
        nodeIndex = index;
    }

    public void SetSelected(bool selected)
    {
        if (visual != null) visual.color = selected ? selectedColor : defaultColor;
    }

    public void SetSimulated(bool dynamic)
    {
        if (isAnchor) return;
        rb.bodyType = dynamic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
    }
}
