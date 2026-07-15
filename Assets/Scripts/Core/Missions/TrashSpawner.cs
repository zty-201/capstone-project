using System.Collections.Generic;
using UnityEngine;

public class TrashSpawner : MonoBehaviour
{
    public static TrashSpawner Instance { get; private set; }

    [Header("Prefab & Spawn Points")]
    [SerializeField] private GameObject trashPrefab;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn Timing")]
    [SerializeField] private float minSpawnInterval = 25f;
    [SerializeField] private float maxSpawnInterval = 45f;

    [Header("Penalty On Spawn")]
    [SerializeField] private int satisfactionPenaltyPerTrash = 5;

    private readonly Dictionary<Transform, TrashPiece> occupied = new Dictionary<Transform, TrashPiece>();
    private readonly List<Transform> freePointsBuffer = new List<Transform>();

    private float spawnTimer;
    private float nextSpawnInterval;

    public bool HasLiveTrash => occupied.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    private void OnEnable() => EventBus.OnDayCompleted += HandleDayCompleted;
    private void OnDisable() => EventBus.OnDayCompleted -= HandleDayCompleted;

    // Trash is town-wide, not mission-scoped. It only needs clearing when satisfaction is about
    // to be force-reset to the next stage's baseline (a real stage pass) — with no refund, since
    // that reset already wipes out whatever a live piece's accumulatedLoss was tracked against.
    // A mission being flagged for review no longer resets satisfaction, so trash is untouched then.
    private void HandleDayCompleted(int day)
    {
        foreach (var piece in occupied.Values)
            if (piece != null) Destroy(piece.gameObject);
        occupied.Clear();
    }

    private void Update()
    {
        if (GameManager.Instance.StateManager.CurrentStateType != GameStateType.Exploration) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= nextSpawnInterval)
        {
            spawnTimer = 0f;
            nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval);
            TrySpawnTrash();
        }
    }

    private void TrySpawnTrash()
    {
        freePointsBuffer.Clear();
        foreach (Transform point in spawnPoints)
        {
            if (!occupied.ContainsKey(point)) freePointsBuffer.Add(point);
        }

        if (freePointsBuffer.Count == 0) return;

        Transform chosen = freePointsBuffer[Random.Range(0, freePointsBuffer.Count)];
        GameObject instance = Instantiate(trashPrefab, chosen.position, Quaternion.identity);
        TrashPiece piece = instance.GetComponent<TrashPiece>();
        piece.Init(this, chosen);
        occupied[chosen] = piece;

        int actualDelta = TownSatisfactionSystem.Instance.ApplyDelta(-satisfactionPenaltyPerTrash);
        piece.TrackLoss(-actualDelta);
    }

    public void RemoveTrash(Transform spawnPoint) => occupied.Remove(spawnPoint);
}
