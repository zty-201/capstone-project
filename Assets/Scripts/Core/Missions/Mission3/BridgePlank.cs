using UnityEngine;

// One placed plank: a physical beam hinged between two BridgeNodes. Kinematic (locked, ignoring
// forces) while the player is still building, switched to Dynamic only for the physics test —
// see BridgeBuilderSystem.StartTest/ResetBridge, which flip every tracked plank and node the
// same way. Both HingeJoint2Ds are wired once at creation and never touched again afterward,
// aside from breakForce reads each Testing frame for the stress visual (see UpdateStressVisual).
[RequireComponent(typeof(Rigidbody2D))]
public class BridgePlank : MonoBehaviour
{
    [Tooltip("The sprite's authored width at localScale 1 — used to stretch the plank exactly across a node-to-node span.")]
    [SerializeField] private float plankLength = 1f;
    [SerializeField] private SpriteRenderer visual;
    [Tooltip("Color every material's plank shifts toward as it approaches its breakForce during the test.")]
    [SerializeField] private Color breakingColor = Color.red;

    private Rigidbody2D rb;
    private HingeJoint2D jointA;
    private HingeJoint2D jointB;
    private float breakForce;
    private Color baseColor;

    public BridgeNode NodeA { get; private set; }
    public BridgeNode NodeB { get; private set; }
    public BridgeMaterialData Material { get; private set; }
    public float Cost { get; private set; }
    public float PlankLength => plankLength;

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    // Stretches the plank sprite between the two nodes and wires a HingeJoint2D to each end.
    // localScale.x = span/plankLength stretches the (assumed horizontal, 1-unit-wide-at-scale-1)
    // sprite to reach exactly from node to node. Cost/breakForce/color all come from material,
    // not fixed constants — this is the whole cost-vs-strength tradeoff the player is choosing.
    public void Setup(BridgeNode nodeA, BridgeNode nodeB, BridgeMaterialData material)
    {
        NodeA = nodeA;
        NodeB = nodeB;
        Material = material;
        breakForce = material.breakForce;
        baseColor = material.plankColor;
        if (visual != null) visual.color = baseColor;

        Vector3 a = nodeA.transform.position;
        Vector3 b = nodeB.transform.position;
        Vector3 mid = (a + b) * 0.5f;
        float span = Vector3.Distance(a, b);
        float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
        Cost = material.costPerUnitLength * span;

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

    // Driven every frame during BridgeBuilderSystem's Testing phase (BridgeBuilderSystem.Update)
    // — colors this plank from its material's own color toward breakingColor as it approaches
    // breakForce, using whichever of its two joints is under more load. A cheap, non-allocating
    // read of the physics engine's already-solved joint state, safe every frame at plank counts
    // this minigame ever reaches.
    public void UpdateStressVisual()
    {
        if (visual == null) return;
        float stress = Mathf.Max(jointA.reactionForce.magnitude, jointB.reactionForce.magnitude) / breakForce;
        visual.color = Color.Lerp(baseColor, breakingColor, Mathf.Clamp01(stress));
    }

    // Unity message: fired automatically when a joint's reaction force exceeds its breakForce.
    // Purely a feedback hook — actual cleanup happens uniformly when BridgeBuilderSystem resets
    // the whole bridge after a failed test, broken or not.
    private void OnJointBreak2D(Joint2D broken) => BridgeBuilderSystem.Instance.NotifyPlankBroken(this);
}
