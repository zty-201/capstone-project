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
    // Spawn interval multiplier per town level (index = TownUpgradeSystem level): each upgrade
    // makes litter pile up less often — the 5S "a sorted town stays clean" payoff for investing.
    [SerializeField] private float[] intervalMultiplierPerTownLevel = { 1f, 2f, 4f };

    private readonly Dictionary<Transform, TrashPiece> occupied = new Dictionary<Transform, TrashPiece>();
    private readonly List<Transform> freePointsBuffer = new List<Transform>();

    private float spawnTimer;
    private float nextSpawnInterval;
    private int townLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        RollNextSpawnInterval();
    }

    private void OnEnable() => EventBus.OnTownUpgraded += HandleTownUpgraded;
    private void OnDisable() => EventBus.OnTownUpgraded -= HandleTownUpgraded;

    // Re-rolls immediately rather than waiting out the old interval, so the upgrade's effect
    // starts right away.
    private void HandleTownUpgraded(int level)
    {
        townLevel = level;
        RollNextSpawnInterval();
    }

    private void RollNextSpawnInterval() =>
        nextSpawnInterval = Random.Range(minSpawnInterval, maxSpawnInterval) * intervalMultiplierPerTownLevel[townLevel];


    private void Update()
    {
        if (GameManager.Instance.StateManager.CurrentStateType != GameStateType.Exploration) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= nextSpawnInterval)
        {
            spawnTimer = 0f;
            RollNextSpawnInterval();
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
    }

    public void RemoveTrash(Transform spawnPoint) => occupied.Remove(spawnPoint);
}
