using UnityEngine;
using UnityEngine.InputSystem;

// Pointer is the common base class for Mouse, Touchscreen, and Pen, so reading
// through it (instead of Mouse.current) makes tap/click handling work unmodified
// on touch-only platforms like Android.
public static class PointerInput
{
    public static bool PrimaryPressedThisFrame()
    {
        return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
    }

    public static bool PrimaryHeld()
    {
        return Pointer.current != null && Pointer.current.press.isPressed;
    }

    public static bool PrimaryReleasedThisFrame()
    {
        return Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;
    }

    public static bool TryGetPrimaryPressWorldPosition(out Vector3 worldPos)
    {
        worldPos = default;
        if (!PrimaryPressedThisFrame()) return false;

        worldPos = ScreenToWorld(Pointer.current.position.ReadValue());
        return true;
    }

    // Unconditional on press state — for continuous tracking while a button is held (e.g. a
    // drag preview, see BridgeBuilderState), where TryGetPrimaryPressWorldPosition's
    // press-this-frame gate would only ever fire once, on the initial press.
    public static bool TryGetPrimaryWorldPosition(out Vector3 worldPos)
    {
        worldPos = default;
        if (Pointer.current == null) return false;

        worldPos = ScreenToWorld(Pointer.current.position.ReadValue());
        return true;
    }

    private static Vector3 ScreenToWorld(Vector2 screenPos)
    {
        float distToPlane = Mathf.Abs(Camera.main.transform.position.z);
        return Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distToPlane));
    }
}
