using System.Collections;
using System.Linq;
using UnityEngine;

// The single minigame for Mission 3 (design doc's "Advanced Mission 1: The Farmer's Broken
// Routine") — a drag-to-reorder UI panel: the player rearranges a row of station cards
// (RoutineCardUI, dragged between RoutineSlotUI drop targets) into the order the farmer should
// actually run his day, then submits. Same "no separate trivial container" shape as Mission 5
// (MissionData.isAdvancedMission): there's only one container and one minigame, and this
// system's own evaluation of the submitted order decides trivial vs. optimal directly (see
// Submit/SimulateAndResolve) rather than the 5 Whys quiz picking a path (see
// PlanningUI.SelectAdvancedMission).
//
// Presented as a popup the same way Mission 5's build/test panel is: Container_Optimal_M3 is a
// screen-space Canvas panel (its own dedicated Canvas, same shape as BridgeCanvas), not a
// world-space container — dragging/dropping is Unity's own IBeginDragHandler/IDragHandler/
// IEndDragHandler/IDropHandler through EventSystem/GraphicRaycaster, which resolves drop targets
// robustly for free instead of this system having to hand-roll its own hit-testing.
public class FarmRoutineSystem : MonoBehaviour
{
    public static FarmRoutineSystem Instance { get; private set; }

    [System.Serializable]
    public class RoutineStation
    {
        public string stationName;
    }

    // Wrapper class purely because Unity can't serialize a jagged int[][] directly in the
    // Inspector — each entry is one accepted station-index permutation, length == stations.Length.
    [System.Serializable]
    public class AcceptedOrder
    {
        public int[] stationOrder;
    }

    [Header("Mission Identity")]
    [SerializeField] private int missionID = 3;
    // Only source of MissionData.minigameHint — same direct-reference shape as
    // RoutineBoardInteractable.associatedMission, not a MissionRegistry lookup, since this
    // component only ever serves the one mission it's configured for.
    [SerializeField] private MissionData missionData;

    [Header("Stations — index here is each station's permanent identity")]
    [SerializeField] private RoutineStation[] stations;
    // Cards/slots below are authored 1:1 against `stations` by array index (cards[i] always
    // represents stations[i]) — same fixed-array-authored-in-parallel shape as
    // PlanningUI.fiveWChoiceButtons / BridgeBuilderUI.materialButtons.
    [SerializeField] private RoutineCardUI[] cards;
    [SerializeField] private RoutineSlotUI[] slots;

    [Header("Win Condition — design doc allows 2 desirable outcomes")]
    [SerializeField] private AcceptedOrder[] acceptedOrders;

    [Header("Attempts")]
    [SerializeField] private int baseAttempts = 5;
    // Bonus attempts per correct answer in the 5 Whys quiz — same idea as
    // BridgeBuilderSystem.bonusAttemptsPerCorrectWhy, so a strong diagnosis still earns something
    // concrete even though this mission's quiz doesn't pick trivial vs. optimal either.
    [SerializeField] private int bonusAttemptsPerCorrectWhy = 1;

    [Header("Simulate (the design doc's per-submit 'small CG')")]
    [SerializeField] private float stepDelay = 0.5f;
    [SerializeField] private float resultHoldDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip stepSfx;
    [SerializeField] private AudioClip successSfx;
    [SerializeField] private AudioClip failSfx;

    private int attemptsUsed;
    private int correctWhysCount;

    public bool IsSimulating { get; private set; }
    public bool CanRearrange => !IsSimulating;
    public string StatusMessage { get; private set; } = "";
    public int MaxAttempts => baseAttempts + correctWhysCount * bonusAttemptsPerCorrectWhy;
    public int RemainingAttempts => MaxAttempts - attemptsUsed;
    public bool ShowHint => StageManager.Instance != null && StageManager.Instance.IsMissionUnderReview(missionID);
    public string HintText { get; private set; } = "";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < cards.Length && i < stations.Length; i++)
            cards[i].Initialize(i, stations[i].stationName);

        // Subscribed in Awake/OnDestroy, not OnEnable/OnDisable: this container disables itself
        // on mission completion, and an OnEnable/OnDisable subscription would unsubscribe right
        // then — leaving nothing listening to hear a later review request's reset signal.
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    private void OnEnable()
    {
        // PlanningUI.SelectAdvancedMission raises OnSolutionSelected (which activates this
        // container, running this OnEnable) strictly before OnFiveWhysCompleted, so subscribing
        // here — not Awake — is enough to always catch it, including on this container's very
        // first-ever activation. Matches BridgeBuilderSystem.OnEnable's same reasoning.
        EventBus.OnFiveWhysCompleted += HandleFiveWhysCompleted;
        ResetRoutine();
    }

    private void OnDisable() => EventBus.OnFiveWhysCompleted -= HandleFiveWhysCompleted;

    // ==========================================
    // DRAG PLACEMENT — called by RoutineSlotUI.OnDrop
    // ==========================================

    public void HandleCardDropped(RoutineCardUI dropped, RoutineSlotUI targetSlot)
    {
        if (!CanRearrange) return;

        RoutineSlotUI sourceSlot = slots.FirstOrDefault(s => s.CurrentCard == dropped);
        if (sourceSlot == null || sourceSlot == targetSlot) return;

        RoutineCardUI occupant = targetSlot.CurrentCard;

        PlaceCardInSlot(dropped, targetSlot);
        if (occupant != null) PlaceCardInSlot(occupant, sourceSlot);
        else sourceSlot.CurrentCard = null;
    }

    private void PlaceCardInSlot(RoutineCardUI card, RoutineSlotUI slot)
    {
        card.transform.SetParent(slot.transform, false);
        ((RectTransform)card.transform).anchoredPosition = Vector2.zero;
        slot.CurrentCard = card;
    }

    // ==========================================
    // SUBMIT / RESOLVE — wired to the panel's Submit button (see RoutineBuilderUI)
    // ==========================================

    public void Submit()
    {
        if (!CanRearrange) return;
        StartCoroutine(SimulateAndResolve());
    }

    private IEnumerator SimulateAndResolve()
    {
        IsSimulating = true;
        int[] order = ReadCurrentOrder();

        // Per-station highlight only — deliberately no OnObjectiveProgress raise per step here:
        // that event's stage-0 slot is already spoken for by M3_BrokenRoutine.optimalObjectives[0]'s
        // "attempts used" phrasing (see below), and reusing it for step-within-simulation count
        // would show a misleading "1/4 attempts used" mid-animation.
        foreach (int stationIndex in order)
        {
            RoutineCardUI card = cards.FirstOrDefault(c => c.StationIndex == stationIndex);
            if (card != null) card.PlayStepHighlight();
            AudioManager.Instance.PlaySFX(stepSfx);
            yield return new WaitForSeconds(stepDelay);
        }

        bool isOptimal = IsAcceptedOrder(order);
        AudioManager.Instance.PlaySFX(isOptimal ? successSfx : failSfx);
        StatusMessage = isOptimal
            ? "The day runs smoothly — every step falls into place."
            : "Something's still out of order...";
        yield return new WaitForSeconds(resultHoldDuration);

        IsSimulating = false;

        if (isOptimal)
        {
            EventBus.RaiseMissionCompleted(missionID, true);
            yield break;
        }

        attemptsUsed++;
        if (attemptsUsed >= MaxAttempts)
        {
            // Out of attempts: the routine never worked out under test, so this mission resolves
            // trivially and waits for a Stage Gate redo (see design doc: "the player will have to
            // wait till next day to retry").
            EventBus.RaiseMissionCompleted(missionID, false);
            yield break;
        }

        StatusMessage = $"Try again — {RemainingAttempts} attempt(s) left.";
        EventBus.RaiseObjectiveProgress(missionID, SolutionType.Optimal, 0, attemptsUsed, MaxAttempts);
    }

    private int[] ReadCurrentOrder() => slots.Select(s => s.CurrentCard.StationIndex).ToArray();

    private bool IsAcceptedOrder(int[] order)
    {
        if (acceptedOrders == null) return false;
        foreach (var accepted in acceptedOrders)
            if (accepted.stationOrder != null && accepted.stationOrder.SequenceEqual(order))
                return true;
        return false;
    }

    // ==========================================
    // RESET — first activation and every Stage Gate redo
    // ==========================================

    private void ResetRoutine()
    {
        attemptsUsed = 0;
        IsSimulating = false;
        StatusMessage = "Arrange the farmer's stations into the right order, then submit.";
        HintText = missionData != null ? missionData.minigameHint : "";

        int[] order = Enumerable.Range(0, stations.Length).ToArray();
        int guard = 0;
        do { Shuffle(order); guard++; }
        while (IsAcceptedOrder(order) && guard < 20); // never hand the player an already-solved board

        for (int i = 0; i < slots.Length && i < order.Length; i++)
        {
            RoutineCardUI card = cards.FirstOrDefault(c => c.StationIndex == order[i]);
            if (card != null) PlaceCardInSlot(card, slots[i]);
        }

        EventBus.RaiseObjectiveProgress(missionID, SolutionType.Optimal, 0, attemptsUsed, MaxAttempts);
    }

    private static void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    private void HandleFiveWhysCompleted(int id, int correctCount)
    {
        if (id != missionID) return;
        correctWhysCount = correctCount;
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        ResetRoutine();
    }
}
