using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform playerTransform; // Reference to calculate starting grid position
    public PathfindingSystem pathfindingSystem;
    public float cellSize = 1f;

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane));

            pathfindingSystem.GetGridCoordinates(worldPos, out int targetX, out int targetY);
            pathfindingSystem.GetGridCoordinates(playerTransform.position, out int startX, out int startY);

            // 1. Check for Interaction
            Collider2D hitCollider = Physics2D.OverlapPoint(worldPos);
            if (hitCollider != null && hitCollider.TryGetComponent(out IInteractable interactable))
            {
                int distance = Mathf.Abs(startX - targetX) + Mathf.Abs(startY - targetY);
                if (distance <= 1)
                {
                    interactable.Interact();
                    return;
                }
                else
                {
                    // Intercept target to stand adjacent to the NPC
                    int dx = startX - targetX;
                    int dy = startY - targetY;

                    if (Mathf.Abs(dx) > Mathf.Abs(dy)) targetX += (dx > 0) ? 1 : -1;
                    else targetY += (dy > 0) ? 1 : -1;
                }
            }

            // 2. Request a Path via Event Bus
            EventBus.OnPathRequested?.Invoke(new Vector2Int(startX, startY), new Vector2Int(targetX, targetY));
        }
    }
}