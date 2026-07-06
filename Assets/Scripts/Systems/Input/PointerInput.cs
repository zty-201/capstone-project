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

    public static bool TryGetPrimaryPressWorldPosition(out Vector3 worldPos)
    {
        worldPos = default;
        if (!PrimaryPressedThisFrame()) return false;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        float distToPlane = Mathf.Abs(Camera.main.transform.position.z);
        worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distToPlane));
        return true;
    }
}
