using System.Collections;
using UnityEngine;

public class NPCPatrol : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float idleTimeMin = 1f;
    public float idleTimeMax = 3f;

    public PathfindingSystem pathfindingSystem;

    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Coroutine patrolCoroutine;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable() => patrolCoroutine = StartCoroutine(PatrolLoop());
    private void OnDisable()
    {
        if (patrolCoroutine != null) StopCoroutine(patrolCoroutine);
    }

    private bool InExploration =>
        GameManager.Instance.StateManager.CurrentStateType == GameStateType.Exploration;

    private IEnumerator PatrolLoop()
    {
        // Wait a frame: OnEnable can run before GameManager's own Awake sets Instance,
        // since Awake/OnEnable order across different objects isn't guaranteed by Unity.
        yield return null;

        while (true)
        {
            if (!InExploration)
            {
                yield return null;
                continue;
            }

            pathfindingSystem.GetGridCoordinates(transform.position, out int cx, out int cy);
            Vector2Int current = new Vector2Int(cx, cy);
            Vector2Int destination = pathfindingSystem.GetRandomWalkableCoordinates(current);
            var path = pathfindingSystem.RequestPathSync(current, destination);

            if (path == null || path.Count < 2)
            {
                yield return null;
                continue;
            }

            yield return StartCoroutine(FollowPath(path));
            yield return new WaitForSeconds(Random.Range(idleTimeMin, idleTimeMax));
        }
    }

    private IEnumerator FollowPath(System.Collections.Generic.List<GridNode> path)
    {
        anim.SetFloat("Speed", 1f);

        for (int i = 1; i < path.Count; i++)
        {
            while (!InExploration) yield return null;

            Vector3 targetPos = pathfindingSystem.GetWorldPositionCenter(path[i].x, path[i].y);

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
        }

        anim.SetFloat("Speed", 0f);
    }
}
