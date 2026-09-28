using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A restock attendant for MarketAmbientSystem's post-mission epilogue. Same movement shape as
// NPCPatrol (RequestPathSync, then step along the path with the move/animate/flip pattern) —
// the only difference is this one is dispatched to a specific destination on demand rather than
// picking a random one on a loop. MarketAmbientSystem holds however many of these the designer
// drags into its `attendants[]` array; nothing here assumes a fixed count or a particular stall.
public class MarketAttendantNPC : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private PathfindingSystem pathfindingSystem;

    private Animator anim;
    private SpriteRenderer spriteRenderer;

    public bool IsAvailable { get; private set; } = true;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private bool InExploration =>
        GameManager.Instance.StateManager.CurrentStateType == GameStateType.Exploration;

    public void DispatchTo(Vector3 targetPosition, Action onArrived)
    {
        if (!IsAvailable) return;
        IsAvailable = false;
        StartCoroutine(MoveToTarget(targetPosition, onArrived));
    }

    private IEnumerator MoveToTarget(Vector3 targetPosition, Action onArrived)
    {
        pathfindingSystem.GetGridCoordinates(transform.position, out int cx, out int cy);
        pathfindingSystem.GetGridCoordinates(targetPosition, out int tx, out int ty);
        List<GridNode> path = pathfindingSystem.RequestPathSync(new Vector2Int(cx, cy), new Vector2Int(tx, ty));

        if (path == null || path.Count < 2)
        {
            // Already standing there, or no route — resolve immediately rather than getting
            // stuck permanently unavailable.
            IsAvailable = true;
            onArrived?.Invoke();
            yield break;
        }

        if (anim != null) anim.SetFloat("Speed", 1f);

        for (int i = 1; i < path.Count; i++)
        {
            while (!InExploration) yield return null;

            Vector3 stepTarget = pathfindingSystem.GetWorldPositionCenter(path[i].x, path[i].y);
            Vector3 moveDir = (stepTarget - transform.position).normalized;

            if (anim != null)
            {
                anim.SetFloat("MoveX", moveDir.x);
                anim.SetFloat("MoveY", moveDir.y);
            }
            if (spriteRenderer != null)
            {
                if (moveDir.x < -0.01f) spriteRenderer.flipX = true;
                else if (moveDir.x > 0.01f) spriteRenderer.flipX = false;
            }

            while (Vector3.Distance(transform.position, stepTarget) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, stepTarget, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = stepTarget;
        }

        if (anim != null) anim.SetFloat("Speed", 0f);

        IsAvailable = true;
        onArrived?.Invoke();
    }
}
