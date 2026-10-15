using UnityEngine;

// Scales a world-space background sprite to exactly fill a target orthographic camera's view —
// the world-space equivalent of a Canvas Image anchored to stretch 0,0-1,1. Needed because a
// container like Container_Optimal_M5 can't be a Canvas child at all (Rigidbody2D-driven objects
// don't track a non-physics parent's transform, and Unity would force a RectTransform on anything
// reparented under a Canvas anyway), so its background doesn't get stretch-anchoring for free the
// way a UI panel's would.
// Resizes through SpriteRenderer.size, not transform.localScale, so a 9-sliced sprite keeps its
// border at native size instead of stretching it — which is why Draw Mode must be Sliced (size
// has no effect in Simple mode), and the sprite's Mesh Type must be Full Rect for slicing to work.
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class FitSpriteToCamera : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private SpriteRenderer spriteRenderer;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

    private void OnEnable() => Fit();

    private void Update()
    {
        // Only recompute when the screen's actual dimensions change (window resize, orientation
        // change) rather than every frame — resizing is cheap but pointless to repeat when
        // nothing about the screen has actually changed.
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            Fit();
    }

    private void Fit()
    {
        if (targetCamera == null || spriteRenderer == null || spriteRenderer.sprite == null) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (spriteRenderer.drawMode != SpriteDrawMode.Sliced)
        {
            Debug.LogError($"[{name}] FitSpriteToCamera needs the SpriteRenderer's Draw Mode set to Sliced.", this);
            return;
        }

        float viewHeight = targetCamera.orthographicSize * 2f;
        float viewWidth = viewHeight * targetCamera.aspect;

        transform.localScale = Vector3.one;
        spriteRenderer.size = new Vector2(viewWidth, viewHeight);
    }
}
