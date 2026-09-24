using System.Collections;
using UnityEngine;

// Mission 4's optimal minigame: a configure-then-simulate puzzle, a new mechanical genre for
// this game (every other optimal minigame is either manipulated continuously — pipe rotation,
// bridge building — or a discrete fetch/assemble/place chain — the winch). The player sets a
// reorder-point threshold per stall (KanbanStallGaugeUI), then runs a simulated market day: each
// stall drains at its own consumption rate and refills after its own delivery lead time once its
// threshold is crossed. Set a threshold too low and the stall runs dry before delivery lands; set
// it too high (above wastefulThresholdRatio) and it's not actually pulling stock on demand, it's
// just always kept topped up — both count as a fail. Passing every stall fires
// RaiseMissionCompleted(4, true) directly; unlike Mission 3/5's Advanced Mission shape, Mission 4
// is a classic mission — the 5 Whys quiz already picked this path, so there's no attempt limit
// here, same as Mission 1's pipe puzzle letting the player retry indefinitely until it's solved.
public class KanbanBuilderSystem : MonoBehaviour
{
    public static KanbanBuilderSystem Instance { get; private set; }

    [System.Serializable]
    public class StallConfig
    {
        public string stallName;
        public float maxStock = 10f;
        public float consumptionRate = 1f;   // stock units drained per simulated second
        public float deliveryLeadTime = 3f;  // seconds between a reorder signal and the delivery landing
        [Range(0f, 1f)] public float wastefulThresholdRatio = 0.75f; // set the reorder point above this and you're just always topped up, not pulling
    }

    [Header("Mission Identity")]
    [SerializeField] private int missionID = 4;

    [Header("Stalls — index here is each stall's permanent identity")]
    [SerializeField] private StallConfig[] stalls;
    // Gauges are authored 1:1 against `stalls` by array index (gauges[i] always represents
    // stalls[i]) — same fixed-array-authored-in-parallel shape as
    // FarmRoutineSystem.stations/cards.
    [SerializeField] private KanbanStallGaugeUI[] gauges;

    [Header("Simulated Day")]
    [SerializeField] private float simDuration = 12f; // real seconds the "day" plays out over

    [Header("Audio")]
    [SerializeField] private AudioClip successSfx;
    [SerializeField] private AudioClip failSfx;

    private float[] currentStock;
    private bool[] pendingDelivery;
    private float[] deliveryTimer;
    private bool[] stallFailed;

    public bool IsSimulating { get; private set; }
    public bool CanConfigure => !IsSimulating;
    public string StatusMessage { get; private set; } = "";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        currentStock = new float[stalls.Length];
        pendingDelivery = new bool[stalls.Length];
        deliveryTimer = new float[stalls.Length];
        stallFailed = new bool[stalls.Length];

        // Subscribed in Awake/OnDestroy, not OnEnable/OnDisable — see MarketStallTrivialSystem's
        // comment on the same pattern.
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    private void OnEnable() => ResetKanban();

    public void RunDay()
    {
        if (!CanConfigure) return;
        StartCoroutine(SimulateDay());
    }

    private IEnumerator SimulateDay()
    {
        IsSimulating = true;
        StatusMessage = "Watching the market run...";

        for (int i = 0; i < stalls.Length; i++)
        {
            currentStock[i] = stalls[i].maxStock;
            pendingDelivery[i] = false;
            deliveryTimer[i] = 0f;
            // A threshold set above the wasteful line fails on principle — it never actually
            // waits for a real pull signal, so nothing that happens later in the day earns it back.
            stallFailed[i] = gauges[i].ThresholdRatio > stalls[i].wastefulThresholdRatio;
        }

        float elapsed = 0f;
        while (elapsed < simDuration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            for (int i = 0; i < stalls.Length; i++)
            {
                StallConfig cfg = stalls[i];
                float thresholdStock = cfg.maxStock * gauges[i].ThresholdRatio;

                currentStock[i] -= cfg.consumptionRate * dt;

                if (!pendingDelivery[i] && currentStock[i] <= thresholdStock)
                {
                    pendingDelivery[i] = true;
                    deliveryTimer[i] = cfg.deliveryLeadTime;
                }
                else if (pendingDelivery[i])
                {
                    deliveryTimer[i] -= dt;
                    if (deliveryTimer[i] <= 0f)
                    {
                        currentStock[i] = cfg.maxStock;
                        pendingDelivery[i] = false;
                    }
                }

                if (currentStock[i] <= 0f) stallFailed[i] = true;
                currentStock[i] = Mathf.Clamp(currentStock[i], 0f, cfg.maxStock);

                gauges[i].SetStockRatio(currentStock[i] / cfg.maxStock, stallFailed[i]);
            }

            yield return null;
        }

        bool allPassed = true;
        foreach (bool failed in stallFailed)
            if (failed) allPassed = false;

        AudioManager.Instance.PlaySFX(allPassed ? successSfx : failSfx);
        StatusMessage = allPassed
            ? "Every stall stayed stocked right when it needed to be — nothing wasted, nothing empty."
            : "Something's still off — a stall either ran dry or you're ordering more than it needs.";

        IsSimulating = false;

        if (allPassed)
            EventBus.RaiseMissionCompleted(missionID, true);
    }

    private void ResetKanban()
    {
        IsSimulating = false;
        StatusMessage = "Set a reorder point for each stall, then run the day.";

        for (int i = 0; i < stalls.Length && i < gauges.Length; i++)
        {
            gauges[i].Initialize(stalls[i].stallName, stalls[i].wastefulThresholdRatio);
            gauges[i].SetThresholdRatio(0.5f);
            gauges[i].SetStockRatio(1f, false);
        }
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        ResetKanban();
    }
}
