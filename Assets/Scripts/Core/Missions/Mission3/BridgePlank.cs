using UnityEngine;

// One placed plank: a physical beam hinged between two BridgeNodes. Kinematic (locked, ignoring
// forces) while the player is still building, switched to Dynamic only for the physics test —
// see BridgeBuilderSystem.StartTest/ResetBridge, which flip every tracked plank and node the
// same way. Both HingeJoint2Ds are wired once at creation and never touched again afterward.
[RequireComponent(typeof(Rigidbody2D))]
public class BridgePlank : MonoBehaviour
{
    [Tooltip("The sprite's authored width at localScale 1 — used to stretch the plank exactly across a node-to-node span.")]
    [SerializeField] private float plankLength = 1f;

    private Rigidbody2D rb;
    private HingeJoint2D jointA;
    private HingeJoint2D jointB;

    public BridgeNode NodeA { get; private set; }
    public BridgeNode NodeB { get; private set; }
    public float PlankLength => plankLength;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    // Stretches the plank sprite between the two nodes and wires a HingeJoint2D to each end.
    // localScale.x = span/plankLength stretches the (assumed horizontal, 1-unit-wide-at-scale-1)
    // sprite to reach exactly from node to node.
    public void Setup(BridgeNode nodeA, BridgeNode nodeB, float breakForce)
    {
        NodeA = nodeA;
        NodeB = nodeB;

        Vector3 a = nodeA.transform.position;
        Vector3 b = nodeB.transform.position;
        Vector3 mid = (a + b) * 0.5f;
        float span = Vector3.Distance(a, b);
        float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

        transform.position = mid;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = new Vector3(span / plankLength, 1f, 1f);

        rb.bodyType = RigidbodyType2D.Kinematic;

        jointA = gameObject.AddComponent<HingeJoint2D>();
        WireJoint(jointA, nodeA, a);

        jointB = gameObject.AddComponent<HingeJoint2D>();
        WireJoint(jointB, nodeB, b);

        jointA.breakForce = breakForce;
        jointB.breakForce = breakForce;
    }

    // connectedBody left null for an anchor node is intentional — Unity treats connectedAnchor
    // as a fixed point in world space in that case, rather than "no connection".
    private void WireJoint(HingeJoint2D joint, BridgeNode node, Vector3 worldAnchor)
    {
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = transform.InverseTransformPoint(worldAnchor);
        joint.connectedBody = node.IsAnchor ? null : node.Body;
        joint.connectedAnchor = node.IsAnchor ? (Vector2)worldAnchor : Vector2.zero;
    }

    public void SetSimulated(bool dynamic) => rb.bodyType = dynamic ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;

    // Unity message: fired automatically when a joint's reaction force exceeds its breakForce.
    // Purely a feedback hook — actual cleanup happens uniformly when BridgeBuilderSystem resets
    // the whole bridge after a failed test, broken or not.
    private void OnJointBreak2D(Joint2D broken) => BridgeBuilderSystem.Instance.NotifyPlankBroken(this);
}
