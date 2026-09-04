using System.Collections.Generic;
using UnityEngine;

// Optimal path for Mission 5 (design doc's "Advanced Mission 3: Bridge Building") — a
// simplified Poly Bridge: the player connects a fixed grid of BridgeNodes with a limited budget
// of BridgePlanks, then a BridgeTestCart drives across to prove the structure holds.
//
// Build vs. Test is implemented as one shared physics scene rather than two separate ones: every
// node/plank is Kinematic (locked at its authored position, ignoring forces) while building, and
// only switched to Dynamic for the test — see StartTest/ResetBridge. A plank's HingeJoint2D
// breakForce (set once at placement in BridgePlank.Setup) is what makes an under-braced bridge
// fail; there is deliberately no separate "is the bridge structurally sound" check beyond the
// cart's own fall — if a joint breaks and that leaves a gap, the cart simply falls through it,
// which the existing fail check already catches.
public class BridgeBuilderSystem : MonoBehaviour
{
    public static BridgeBuilderSystem Instance { get; private set; }

    public enum BuildPhase { Building, Testing }

    [Header("Mission Identity")]
    [SerializeField] private int missionID = 5;

    [Header("Budget")]
    [SerializeField] private int plankBudget = 8;
    [SerializeField] private float maxPlankLength = 3f;
    [SerializeField] private float plankBreakForce = 40f;

    [Header("Prefab & Scene References")]
    [SerializeField] private BridgePlank plankPrefab;
    [SerializeField] private Transform planksParent;
    [SerializeField] private BridgeTestCart cart;
    [SerializeField] private Transform cartStartPoint;
    [SerializeField] private Transform goalMarker;
    [SerializeField] private float failY = -6f;
    [SerializeField] private float maxTestDuration = 20f;

    [Header("UI")]
    [SerializeField] private GameObject uiPanel;

    [Header("Audio")]
    [SerializeField] private AudioClip placeSfx;
    [SerializeField] private AudioClip removeSfx;
    [SerializeField] private AudioClip breakSfx;

    // Cached in Awake, not Start: this object lives inside Container_Optimal_M5, which starts
    // inactive, and OnEnable (which needs this list immediately, via ResetBridge) fires the
    // moment the container activates — before Start would. Awake still runs once at scene load
    // even for an initially-inactive object, same as every other cached-at-startup mission system.
    private BridgeNode[] nodes;

    private readonly List<BridgePlank> placedPlanks = new List<BridgePlank>();
    private readonly Dictionary<(int, int), BridgePlank> plankLookup = new Dictionary<(int, int), BridgePlank>();

    private BridgeNode selectedNode;
    private int planksUsed;
    private float testTimer;

    public BuildPhase Phase { get; private set; } = BuildPhase.Building;
    public int RemainingPlanks => plankBudget - planksUsed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        nodes = GetComponentsInChildren<BridgeNode>(true);
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    private void OnEnable()
    {
        if (uiPanel != null) uiPanel.SetActive(true);
        ResetBridge();
    }

    private void OnDisable()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
    }

    private void Update()
    {
        if (Phase != BuildPhase.Testing) return;

        testTimer += Time.deltaTime;

        if (cart.transform.position.y < failY || testTimer >= maxTestDuration)
        {
            HandleTestFailed();
            return;
        }

        if (goalMarker != null && cart.transform.position.x >= goalMarker.position.x)
            HandleTestSucceeded();
    }

    // Called by BridgeNode when a click lands on it. Select-then-connect: the first click picks
    // a node, the second either places a plank to it, removes an existing one (toggle), or does
    // nothing if the pair is invalid (too far, or budget exhausted).
    public void HandleNodeClicked(BridgeNode node)
    {
        if (Phase != BuildPhase.Building) return;

        if (selectedNode == null)
        {
            selectedNode = node;
            node.SetSelected(true);
            return;
        }

        BridgeNode first = selectedNode;
        selectedNode.SetSelected(false);
        selectedNode = null;

        if (first == node) return; // clicked the same node twice: just deselect

        var key = MakeKey(first.NodeIndex, node.NodeIndex);
        if (plankLookup.TryGetValue(key, out BridgePlank existing))
        {
            RemovePlank(key, existing);
            return;
        }

        if (RemainingPlanks <= 0) return;
        if (Vector3.Distance(first.transform.position, node.transform.position) > maxPlankLength) return;

        PlacePlank(key, first, node);
    }

    private void PlacePlank((int, int) key, BridgeNode a, BridgeNode b)
    {
        BridgePlank plank = Instantiate(plankPrefab, planksParent);
        plank.Setup(a, b, plankBreakForce);

        placedPlanks.Add(plank);
        plankLookup[key] = plank;
        planksUsed++;

        AudioManager.Instance.PlaySFX(placeSfx);
        RaiseBuildProgress();
    }

    private void RemovePlank((int, int) key, BridgePlank plank)
    {
        placedPlanks.Remove(plank);
        plankLookup.Remove(key);
        planksUsed--;
        Destroy(plank.gameObject);

        AudioManager.Instance.PlaySFX(removeSfx);
        RaiseBuildProgress();
    }

    private static (int, int) MakeKey(int a, int b) => a < b ? (a, b) : (b, a);

    private void RaiseBuildProgress()
        => EventBus.RaiseObjectiveProgress(missionID, SolutionType.Optimal, 0, planksUsed, plankBudget);

    // Feedback hook only — see the class comment on why a break doesn't need its own handling.
    public void NotifyPlankBroken(BridgePlank plank) => AudioManager.Instance.PlaySFX(breakSfx);

    // Wired to the Test button's OnClick in the Inspector.
    public void StartTest()
    {
        if (Phase != BuildPhase.Building) return;

        Phase = BuildPhase.Testing;
        testTimer = 0f;

        foreach (var node in nodes) node.SetSimulated(true);
        foreach (var plank in placedPlanks) plank.SetSimulated(true);

        cart.ResetToStart(cartStartPoint.position);
        cart.BeginDrive();

        EventBus.RaiseObjectiveProgress(missionID, SolutionType.Optimal, 1, 0, 0);
    }

    // Wired to the Reset button's OnClick in the Inspector, and called automatically after
    // every failed test and on container activation/review — one way to get back to a clean
    // Building state, not a partial-vs-full special case.
    public void ResetBridge()
    {
        Phase = BuildPhase.Building;
        testTimer = 0f;

        if (selectedNode != null) { selectedNode.SetSelected(false); selectedNode = null; }

        foreach (var plank in placedPlanks) Destroy(plank.gameObject);
        placedPlanks.Clear();
        plankLookup.Clear();
        planksUsed = 0;

        foreach (var node in nodes) node.ResetToOriginal();
        if (cart != null && cartStartPoint != null) cart.ResetToStart(cartStartPoint.position);

        RaiseBuildProgress();
    }

    private void HandleTestFailed()
    {
        cart.StopDrive();
        ResetBridge();
    }

    private void HandleTestSucceeded()
    {
        cart.StopDrive();
        EventBus.RaiseMissionCompleted(missionID, true);
    }

    private void HandleMissionsNeedReview(int[] missionIDs)
    {
        if (System.Array.IndexOf(missionIDs, missionID) < 0) return;
        ResetBridge();
    }
}
