using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Mission 4's single minigame (design doc's "Tangled Marketplace"): a configure-then-simulate
// puzzle, a new mechanical genre for this game (every other optimal minigame is either
// manipulated continuously — pipe rotation, bridge building — or a discrete fetch/assemble/place
// chain — the winch). The player sets a reorder-point threshold per stall (KanbanStallGaugeUI),
// then runs a simulated market day: each stall drains at its own consumption rate and refills
// after its own delivery lead time once its threshold is crossed. Set a threshold too low and the
// stall runs dry before delivery lands; set it too high (above wastefulThresholdRatio) and it's
// not actually pulling stock on demand, it's just always kept topped up — both count as a fail.
//
// Mission 4 is an Advanced Mission (MissionData.isAdvancedMission), same shape as Mission 3/5:
// there's no separate trivial-path container, and the 5 Whys quiz doesn't pick SolutionType at
// all — PlanningUI.SelectAdvancedMission() always routes into this single container, and this
// system's own pass/fail on the simulated day decides wasOptimal directly. The quiz score instead
// buys bonus attempts (same "quiz score becomes practice attempts, not the decision itself" idea
// FarmRoutineSystem/BridgeBuilderSystem use) — a multiple-choice diagnosis can't stand in for
// "did you actually tune the thresholds correctly" any better than it can for "does the bridge
// hold" or "is the chore order right," so the test itself is what proves it.
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

    // Tracked per stall instead of a single bool so the end-of-day message can tell the player
    // *which* of the two failure modes happened and in which direction to adjust — "it failed"
    // alone gives no way to know whether to raise or lower a threshold.
    private enum FailureReason { None, Stockout, Wasteful }

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

    [Header("Attempts")]
    [SerializeField] private int baseAttempts = 5;
    // Bonus attempts per correct answer in the 5 Whys quiz — same idea as
    // FarmRoutineSystem.bonusAttemptsPerCorrectWhy/BridgeBuilderSystem.bonusAttemptsPerCorrectWhy,
    // so a strong diagnosis still earns something concrete even though this mission's quiz
    // doesn't pick the path either.
    [SerializeField] private int bonusAttemptsPerCorrectWhy = 1;

    [Header("Audio")]
    [SerializeField] private AudioClip successSfx;
    [SerializeField] private AudioClip failSfx;

    private float[] currentStock;
    private bool[] pendingDelivery;
    private float[] deliveryTimer;
    private FailureReason[] failureReason;
    private float dayElapsed;
    private int attemptsUsed;
    private int correctWhysCount;

    public bool IsSimulating { get; private set; }
    public bool CanConfigure => !IsSimulating;
    public string StatusMessage { get; private set; } = "";
    public int StallCount => stalls.Length;
    public int MaxAttempts => baseAttempts + correctWhysCount * bonusAttemptsPerCorrectWhy;
    public int RemainingAttempts => MaxAttempts - attemptsUsed;

    // Read by KanbanBuilderUI to show a "Day: 60%" readout while a run is in progress — purely
    // cosmetic, so the player understands this is a timed run with a defined end rather than
    // indefinite ambient motion.
    public float DayProgress01 => simDuration > 0f ? Mathf.Clamp01(dayElapsed / simDuration) : 0f;

    // Read by MarketAmbientSystem once the mission resolves optimally, so the permanent
    // post-completion "Kanban mode" epilogue restocks each stall at the exact threshold the
    // player actually tuned here — not a fresh, independently-authored "ideal" value. The
    // container is inactive by then (MinigameActivator disables it on completion), but an
    // inactive GameObject's components keep their field values, so this still reads correctly.
    public float GetThresholdRatio(int index) => gauges[index].ThresholdRatio;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        currentStock = new float[stalls.Length];
        pendingDelivery = new bool[stalls.Length];
        deliveryTimer = new float[stalls.Length];
        failureReason = new FailureReason[stalls.Length];

        // Subscribed in Awake/OnDestroy, not OnEnable/OnDisable — see MarketStallTrivialSystem's
        // comment on the same pattern.
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    private void OnEnable()
    {
        // PlanningUI.SelectAdvancedMission raises OnSolutionSelected (which activates this
        // container, running this OnEnable) strictly before OnFiveWhysCompleted, so subscribing
        // here — not Awake — is enough to always catch it, including on this container's very
        // first-ever activation. Matches FarmRoutineSystem/BridgeBuilderSystem's same reasoning.
        EventBus.OnFiveWhysCompleted += HandleFiveWhysCompleted;
        ResetKanban();
    }

    private void OnDisable() => EventBus.OnFiveWhysCompleted -= HandleFiveWhysCompleted;

    public void RunDay()
    {
        if (!CanConfigure) return;
        StartCoroutine(SimulateDay());
    }

    private IEnumerator SimulateDay()
    {
        IsSimulating = true;
        StatusMessage = "Watching the market run...";
        dayElapsed = 0f;

        for (int i = 0; i < stalls.Length; i++)
        {
            currentStock[i] = stalls[i].maxStock;
            pendingDelivery[i] = false;
            deliveryTimer[i] = 0f;
            // A threshold set above the wasteful line fails on principle — it never actually
            // waits for a real pull signal, so nothing that happens later in the day earns it back.
            failureReason[i] = gauges[i].ThresholdRatio > stalls[i].wastefulThresholdRatio
                ? FailureReason.Wasteful
                : FailureReason.None;
        }

        while (dayElapsed < simDuration)
        {
            float dt = Time.deltaTime;
            dayElapsed += dt;

            for (int i = 0; i < stalls.Length; i++)
            {
                StallConfig cfg = stalls[i];
                float thresholdStock = cfg.maxStock * gauges[i].ThresholdRatio;

                currentStock[i] -= cfg.consumptionRate * dt;

                if (!pendingDelivery[i] && currentStock[i] <= thresholdStock)
                {
                    pendingDelivery[i] = true;
                    deliveryTimer[i] = cfg.deliveryLeadTime;
                    gauges[i].ShowEventCallout("Reordered!");
                }
                else if (pendingDelivery[i])
                {
                    deliveryTimer[i] -= dt;
                    if (deliveryTimer[i] <= 0f)
                    {
                        currentStock[i] = cfg.maxStock;
                        pendingDelivery[i] = false;
                        gauges[i].ShowEventCallout("Restocked!");
                    }
                }

                // Only flags on the actual transition into stockout, and only if nothing already
                // flagged this stall (a Wasteful threshold set at day-start shouldn't be
                // overwritten just because the stall also happened to run dry).
                if (currentStock[i] <= 0f && failureReason[i] == FailureReason.None)
                {
                    failureReason[i] = FailureReason.Stockout;
                    gauges[i].ShowEventCallout("Ran dry!");
                }
                currentStock[i] = Mathf.Clamp(currentStock[i], 0f, cfg.maxStock);

                gauges[i].SetStockRatio(currentStock[i] / cfg.maxStock);
            }

            yield return null;
        }

        bool allPassed = true;
        var failureLines = new List<string>();
        for (int i = 0; i < stalls.Length; i++)
        {
            bool failed = failureReason[i] != FailureReason.None;
            gauges[i].SetFailureOutline(failed);
            if (!failed) continue;

            allPassed = false;
            failureLines.Add(failureReason[i] == FailureReason.Stockout
                ? $"{stalls[i].stallName} ran dry — try raising its reorder point."
                : $"{stalls[i].stallName} is set too cautiously — try lowering it.");
        }

        AudioManager.Instance.PlaySFX(allPassed ? successSfx : failSfx);
        IsSimulating = false;

        if (allPassed)
        {
            StatusMessage = "Every stall stayed stocked right when it needed to be — nothing wasted, nothing empty.";
            EventBus.RaiseMissionCompleted(missionID, true);
            yield break;
        }

        attemptsUsed++;
        if (attemptsUsed >= MaxAttempts)
        {
            // Out of attempts: the system never held up under test, so this mission resolves
            // trivially and waits for its rushed fix to break down (MissionReviewSystem), same "exhausted attempts" shape
            // BridgeBuilderSystem/FarmRoutineSystem use.
            StatusMessage = string.Join("\n", failureLines) + "\nOut of attempts.";
            EventBus.RaiseMissionCompleted(missionID, false);
            yield break;
        }

        StatusMessage = string.Join("\n", failureLines) + $"\n{RemainingAttempts} attempt(s) left.";
        EventBus.RaiseObjectiveProgress(missionID, SolutionType.Optimal, 0, attemptsUsed, MaxAttempts);
    }

    private void ResetKanban()
    {
        IsSimulating = false;
        attemptsUsed = 0;
        StatusMessage = "Set a reorder point for each stall, then run the day.";

        for (int i = 0; i < stalls.Length && i < gauges.Length; i++)
        {
            gauges[i].Initialize(stalls[i].stallName, stalls[i].wastefulThresholdRatio);
            gauges[i].SetThresholdRatio(0.5f);
            gauges[i].SetStockRatio(1f);
            gauges[i].SetFailureOutline(false);
        }

        EventBus.RaiseObjectiveProgress(missionID, SolutionType.Optimal, 0, attemptsUsed, MaxAttempts);
    }

    private void HandleFiveWhysCompleted(int id, int correctCount)
    {
        if (id != missionID) return;
        correctWhysCount = correctCount;
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        ResetKanban();
    }
}
