using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    [SerializeField] private LayerMask npcLayerMask;

    private Coroutine movementCoroutine;
    private Vector2Int currentDestination;

    private Animator anim;
    private SpriteRenderer spriteRenderer;

    public PathfindingSystem pathfindingSystem;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() => EventBus.OnPathGenerated += HandlePathGenerated;
    private void OnDisable() => EventBus.OnPathGenerated -= HandlePathGenerated;

    private void HandlePathGenerated(List<GridNode> path)
    {
        if (path.Count > 0)
            currentDestination = new Vector2Int(path[path.Count - 1].x, path[path.Count - 1].y);

        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(FollowPath(path));
    }

    private IEnumerator FollowPath(List<GridNode> path)
    {
        anim.SetFloat("Speed", 1f);

        for (int i = 1; i < path.Count; i++)
        {
            bool isFinalNode = i == path.Count - 1;
            Vector3 targetPos = pathfindingSystem.GetWorldPositionCenter(path[i].x, path[i].y);

            if (!isFinalNode)
            {
                Collider2D hit = Physics2D.OverlapPoint(targetPos, npcLayerMask);
                if (hit != null)
                {
                    pathfindingSystem.GetGridCoordinates(transform.position, out int cx, out int cy);
                    EventBus.RaisePathRequested(new Vector2Int(cx, cy), currentDestination);
                    yield break;
                }
            }

            Vector3 moveDir = (targetPos - transform.position).normalized;
            anim.SetFloat("MoveX", moveDir.x);
            anim.SetFloat("MoveY", moveDir.y);

            if (moveDir.x < -0.01f) spriteRenderer.flipX = true;
            else if (moveDir.x > 0.01f) spriteRenderer.flipX = false;

            while (Vector3.Distance(transform.position, targetPos) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPos;
            EventBus.RaisePlayerMoved(new Vector2Int(path[i].x, path[i].y));
        }

        anim.SetFloat("Speed", 0f);
    }
}