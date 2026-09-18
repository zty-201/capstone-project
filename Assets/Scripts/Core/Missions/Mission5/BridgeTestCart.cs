using UnityEngine;

// The "mini car" that physically drives across the built bridge to test it — a simplified
// Poly Bridge stand-in with plain constant-velocity locomotion rather than full wheel physics.
// Dumb on purpose: BridgeBuilderSystem owns every win/fail check (goal/fall thresholds) and
// just tells this component when to start/stop/reset moving.
[RequireComponent(typeof(Rigidbody2D))]
public class BridgeTestCart : MonoBehaviour
{
    [SerializeField] private float driveSpeed = 2f;

    private Rigidbody2D rb;
    private bool isDriving;

    // Lazily fetched rather than cached only in Awake: BridgeBuilderSystem.OnEnable() (on the
    // container root) calls ResetToStart via ResetBridge() synchronously during the same
    // container.SetActive(true) that activates this object too — but Unity only guarantees an
    // object's own Awake precedes its own OnEnable, never one object's Awake before a *different*
    // object's OnEnable, even parent/child activated together (verified: cross-object Awake/
    // OnEnable order is explicitly undefined). This object's Awake losing that race is exactly
    // what caused a NullReferenceException here — a lazy getter is correct regardless of which
    // object's lifecycle method Unity happens to run first.
    private Rigidbody2D Rb => rb ??= GetComponent<Rigidbody2D>();

    public void BeginDrive()
    {
        isDriving = true;
        Rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void StopDrive()
    {
        isDriving = false;
        Rb.linearVelocity = Vector2.zero;
    }

    public void ResetToStart(Vector3 startPosition)
    {
        isDriving = false;
        Rb.bodyType = RigidbodyType2D.Kinematic;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        Rb.linearVelocity = Vector2.zero;
        Rb.angularVelocity = 0f;
    }

    private void FixedUpdate()
    {
        if (!isDriving) return;
        Rb.linearVelocity = new Vector2(driveSpeed, Rb.linearVelocity.y);
    }
}
