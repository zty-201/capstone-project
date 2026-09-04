using UnityEngine;

// A fixed click-target in the bridge builder's node grid (see BridgeBuilderSystem). Anchor
// nodes (isAnchor) are solid ground — a pure Transform + trigger collider, referenced by a
// plank's HingeJoint2D as a fixed world-space point (Unity treats a joint's connectedAnchor as
// a world position whenever connectedBody is left null). Deck nodes carry their own Rigidbody2D
// so every plank meeting at the same point moves together as one hinge; it's Kinematic (locked
// in place, ignoring forces) while the player is building and only switched to Dynamic for the
// physics test, same lifecycle as every BridgePlank — see BridgeBuilderSystem.StartTest/ResetBridge.
// Needs a Collider2D (CircleCollider2D recommended, set to Is Trigger) for click detection —
// RequireComponent can't target Collider2D itself since it's abstract, so this is enforced by
// convention (matching every other click-hit-tested object in the codebase, e.g. PipeVisual).
public class BridgeNode : MonoBehaviour
{
    [SerializeField] private int nodeIndex;
    [SerializeField] private bool isAnchor;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    private Collider2D col;
    private Rigidbody2D rb;
    private Vector3 originalPosition;

    public int NodeIndex => nodeIndex;
    public bool IsAnchor => isAnchor;
    public Rigidbody2D Body => rb;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        if (!isAnchor) rb = GetComponent<Rigidbody2D>();
        originalPosition = transform.position;
    }

    private void OnEnable() => EventBus.OnBridgeClicked += HandleBridgeClicked;
    private void OnDisable() => EventBus.OnBridgeClicked -= HandleBridgeClicked;

    private void HandleBridgeClicked(Vector3 worldPos)
    {
        if (GameManager.Instance.StateManager.CurrentStateType != GameStateType.BridgeBuilder) return;
        if (col.OverlapPoint(worldPos)) BridgeBuilderSystem.Instance.HandleNodeClicked(this);
    }

    public void SetSelected(bool selected)
    {
        if (visual != null) visual.color = selected ? selectedColor : defaultColor;
    }

    // Only meaningful for non-anchor nodes — anchors never move.
    public void ResetToOriginal()
    {
        if (isAnchor) return;
        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.position = originalPosition;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    public void SetSimulated(bool dynamic)
    {
        if (isAnchor) return;
        rb.bodyType = dynamic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
    }
}
