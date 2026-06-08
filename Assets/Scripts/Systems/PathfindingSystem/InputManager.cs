using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("Dependencies")]
    public Transform playerTransform;
    public PathfindingSystem pathfindingSystem;
    public float cellSize = 1f;

    private void OnEnable()
    {
        // Subscribe to the State Machine's broadcast
        EventBus.OnMapClicked += HandleMapClicked;
    }

    private void OnDisable()
    {
        EventBus.OnMapClicked -= HandleMapClicked;
    }

    private void HandleMapClicked(Vector3 worldPos)
    {
        if (pathfindingSystem == null || playerTransform == null) return;

        // Convert world positions to grid coordinates
        pathfindingSystem.GetGridCoordinates(worldPos, out int targetX, out int targetY);
        pathfindingSystem.GetGridCoordinates(playerTransform.position, out int startX, out int startY);

        // 1. Check for Interaction using your original Physics check
        Collider2D hitCollider = Physics2D.OverlapPoint(worldPos);
        if (hitCollider != null && hitCollider.TryGetComponent(out IInteractable interactable))
        {
            int distance = Mathf.Abs(startX - targetX) + Mathf.Abs(startY - targetY);

            if (distance <= 1)
            {
                // If standing next to it, interact immediately and do not move
                interactable.Interact();
                return;
            }
            else
            {
                // Intercept target to stand adjacent to the NPC using your original math
                int dx = startX - targetX;
                int dy = startY - targetY;

                if (Mathf.Abs(dx) > Mathf.Abs(dy)) targetX += (dx > 0) ? 1 : -1;
                else targetY += (dy > 0) ? 1 : -1;
            }
        }

        // 2. Request a Path via your original Event Bus signal
        EventBus.OnPathRequested?.Invoke(new Vector2Int(startX, startY), new Vector2Int(targetX, targetY));
    }
}