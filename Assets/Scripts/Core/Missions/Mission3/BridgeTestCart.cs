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

    private void Awake() => rb = GetComponent<Rigidbody2D>();

    public void BeginDrive()
    {
        isDriving = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void StopDrive()
    {
        isDriving = false;
        rb.linearVelocity = Vector2.zero;
    }

    public void ResetToStart(Vector3 startPosition)
    {
        isDriving = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void FixedUpdate()
    {
        if (!isDriving) return;
        rb.linearVelocity = new Vector2(driveSpeed, rb.linearVelocity.y);
    }
}
