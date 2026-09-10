using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

// The single minigame for Mission 5 (design doc's "Advanced Mission 3: Bridge Building") — a
// full Poly Bridge: the player drags to freely place BridgeNodes and BridgePlanks (choosing from
// several BridgeMaterialData tradeoffs) within a cost budget, then a BridgeTestCart drives across
// to prove the structure holds. There is no separate trivial-path minigame for advanced missions
// (MissionData.isAdvancedMission): the 5 Whys quiz no longer picks a path (see
// PlanningUI.SelectAdvancedMission) — it only grants bonus test attempts via
// OnFiveWhysCompleted — and this system's own test outcome decides trivial vs. optimal directly.
// A passed test raises RaiseMissionCompleted(missionID, true); running out of attempts (see
// HandleTestFailed) raises RaiseMissionCompleted(missionID, false) — the "quick, unreinforced
// bridge" outcome — with no separate fetch-quest ever played.
//
// Build vs. Test is implemented as one shared physics scene rather than two separate ones: every
// node/plank is Kinematic (locked at its authored position, ignoring forces) while building, and
// only switched to Dynamic for the test — see StartTest/ResetBridge. A plank's HingeJoint2D
// breakForce (set once at placement from its BridgeMaterialData) is what makes an under-braced
// bridge fail; there is deliberately no separate "is the bridge structurally sound" check beyond
// the cart's own fall — if a joint breaks and that leaves a gap, the cart simply falls through
// it, which the existing fail check already catches.
//
// Presented as a popup, same idea as the pipe puzzle's Container_Optimal_M1 (a background sprite
// plus interactive sprites, always framed the same way regardless of where the player triggered
// it) — but reached the opposite way round. The pipe puzzle can get away with a CameraFollower
// on its container because it has zero Rigidbody2D anywhere; this container is nothing BUT
// Rigidbody2D-driven objects (nodes, planks, the cart), and Unity does not carry a moving
// non-physics parent's motion into a Rigidbody2D child — the child's transform gets corrected
// back to hold its world position, so a CameraFollower here would leave the physics objects
// stuck in place while only the (non-physics) background moved. So instead the container itself
// never moves — it stays at its authored map position exactly like every other minigame
// container — and BridgeBuilderState swaps to a second, dedicated Cinemachine camera
// (bridgeViewCamera) framing that fixed spot while this state is active, swapping back to
// playerCamera on Exit. See BridgeBuilderState.Enter/Exit.
//
// Free placement: only anchor nodes are Editor-authored; every deck node is created at runtime
// by a drag (see HandleDragStart/Update/End) that resolves each end to either an already-existing
// point or a snapped, brand-new one — never both new (at least one end of a beam must already
// exist). Node click/overlap resolution is centralized here (FindNearestNode) rather than
// decentralized per-node, since snapping to empty grid space is something no single node's own
// collider could ever answer about itself. Undo/redo (see BridgeActions.cs) wraps every
// placement/deletion as a small reversible action on two stacks, cleared on ResetBridge — a
// stale entry referencing an already-destroyed object would corrupt state otherwise, and since
// HandleTestFailed already routes through ResetBridge, "don't persist history across a failed
// test" falls out for free rather than needing separate handling.
public class BridgeBuilderSystem : MonoBehaviour
{
    public static BridgeBuilderSystem Instance { get; private set; }

    public enum BuildPhase { Building, Testing }

    // Below the drag start/release distance, a gesture is treated as a click (select a node for
    // deletion) rather than an attempt to place a beam.
    private const float ClickDragThreshold = 0.15f;

    [Header("Mission Identity")]
    [SerializeField] private int missionID = 5;

    [Header("Budget")]
    [SerializeField] private float budget = 20f;
    [SerializeField] private float maxPlankLength = 3f;

    [Header("Test Attempts")]
    [SerializeField] private int baseTestAttempts = 3;
    // Bonus test attempts per correct answer in the 5 Whys quiz (see OnFiveWhysCompleted) — this
    // mission doesn't use that quiz to pick trivial vs. optimal, so a strong diagnosis earns more
    // room to get the actual build right instead.
    [SerializeField] private int bonusAttemptsPerCorrectWhy = 1;

    [Header("Materials")]
    [SerializeField] private BridgeMaterialData[] materials;

    [Header("Placement")]
    [SerializeField] private BridgeNode nodePrefab;
    [SerializeField] private Transform nodesParent;
    [SerializeField] private float gridSpacing = 0.5f;
    // Recomputed every OnEnable from bridgeViewCamera's own framing (see ComputePlaygroundBounds)
    // — not hand-typed. bridgeViewCamera already defines exactly how much of the playground is
    // visible, so deriving bounds from it keeps them correct by construction instead of needing
    // to be kept in sync by hand if the camera's framing ever changes. Whatever value shows here
    // in the Inspector before Play is just the last computed result (or the unset default before
    // this ever ran once) — editing it directly has no lasting effect.
    [SerializeField] private Rect playgroundBounds;
    [SerializeField] private float playgroundMargin = 1f;
    [SerializeField] private float nodeSnapRadius = 0.3f;
    [SerializeField] private LineRenderer previewLine;

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

    [Header("Camera")]
    // The scene's normal player-tracking Cinemachine camera, and a second, dedicated one framing
    // this container's fixed map position — BridgeBuilderState swaps between them on Enter/Exit
    // (see the class comment above for why the swap happens here and not by moving this
    // container itself).
    [SerializeField] private GameObject playerCamera;
    [SerializeField] private GameObject bridgeViewCamera;
    // Cinemachine 3's CinemachineCamera has no Culling Mask of its own — it only blends
    // position/rotation/lens into the one real Camera (Main Camera, via CinemachineBrain), never
    // the Culling Mask. So neither layer's visibility can be controlled per-vcam the way
    // playerCamera/bridgeViewCamera's active state is above; ShowBridgeLayers/HideBridgeLayers
    // toggle both directly on Camera.main instead — see BridgeBuilderState.Enter/Exit.
    [SerializeField] private string bridgeLayerName = "Bridge";
    // Reinforcement-only planks (BridgeMaterialData.isRoad == false) live on this separate layer
    // instead — see PlacePlankInternal. Configured in Project Settings > Physics 2D > Layer
    // Collision Matrix to never collide with bridgeLayerName (where the cart itself lives), so a
    // reinforcement beam can brace a span without the cart ever mistaking it for road; it still
    // fully participates in HingeJoint2D load-bearing regardless, since that's a separate system
    // from collision detection entirely.
    [SerializeField] private string bridgeSupportLayerName = "BridgeSupport";

    [Header("Audio")]
    [SerializeField] private AudioClip placeSfx;
    [SerializeField] private AudioClip removeSfx;
    [SerializeField] private AudioClip breakSfx;

    // Growable, unlike a fixed array, since deck nodes are created and destroyed at runtime.
    // Cached in Awake, not Start: this object lives inside Container_Optimal_M5, which starts
    // inactive, so Awake is deferred until the container's first activation rather than running
    // at scene load — but Awake still always precedes OnEnable within that same activation, and
    // OnEnable (via ResetBridge) needs this list immediately, before Start would ever run.
    private readonly List<BridgeNode> nodes = new List<BridgeNode>();

    private readonly List<BridgePlank> placedPlanks = new List<BridgePlank>();
    private readonly Dictionary<(int, int), BridgePlank> plankLookup = new Dictionary<(int, int), BridgePlank>();
    private readonly Stack<IBridgeAction> undoStack = new Stack<IBridgeAction>();
    private readonly Stack<IBridgeAction> redoStack = new Stack<IBridgeAction>();

    private BridgeNode selectedNode;
    private float budgetUsed;
    private float testTimer;
    private int correctWhysCount;
    private int attemptsUsed;
    private int nextNodeIndex;

    private bool isDragging;
    private Vector3 rawPressWorldPos;
    private BridgeNode dragStartNode;
    private Vector3 dragStartPoint;

    public BuildPhase Phase { get; private set; } = BuildPhase.Building;
    public float RemainingBudget => budget - budgetUsed;
    public int MaxTestAttempts => baseTestAttempts + correctWhysCount * bonusAttemptsPerCorrectWhy;
    public int RemainingAttempts => MaxTestAttempts - attemptsUsed;
    public GameObject PlayerCamera => playerCamera;
    public GameObject BridgeViewCamera => bridgeViewCamera;
    public BridgeMaterialData[] Materials => materials;
    public BridgeMaterialData SelectedMaterial { get; private set; }
    public BridgeNode SelectedNode => selectedNode;
    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        nodes.AddRange(GetComponentsInChildren<BridgeNode>(true));
        nextNodeIndex = nodes.Count > 0 ? nodes.Max(n => n.NodeIndex) + 1 : 0;
        EventBus.OnMissionsNeedReview += HandleMissionsNeedReview;
    }

    private void OnDestroy() => EventBus.OnMissionsNeedReview -= HandleMissionsNeedReview;

    private void OnEnable()
    {
        // PlanningUI.SelectAdvancedMission raises OnSolutionSelected (which activates this
        // container, running this OnEnable) strictly before OnFiveWhysCompleted, so subscribing
        // here — not Awake — is enough to always catch it, including on this container's very
        // first-ever activation.
        EventBus.OnFiveWhysCompleted += HandleFiveWhysCompleted;

        if (uiPanel != null) uiPanel.SetActive(true);
        attemptsUsed = 0;
        SelectedMaterial = materials != null && materials.Length > 0 ? materials[0] : null;
        ComputePlaygroundBounds();
        ResetBridge();
    }

    // bridgeViewCamera already defines exactly how much of the playground the player can see —
    // deriving the placement bounds from its actual orthographic size/aspect every activation
    // means "you can build anywhere you can see" is true by construction, rather than needing a
    // hand-typed Rect to be kept in sync with the camera's framing by hand.
    private void ComputePlaygroundBounds()
    {
        if (bridgeViewCamera == null || Camera.main == null) return;
        CinemachineCamera vcam = bridgeViewCamera.GetComponent<CinemachineCamera>();
        if (vcam == null) return;

        float halfHeight = vcam.Lens.OrthographicSize - playgroundMargin;
        float halfWidth = halfHeight * Camera.main.aspect;
        Vector3 center = bridgeViewCamera.transform.position;

        playgroundBounds = new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
    }

    private void OnDisable()
    {
        EventBus.OnFiveWhysCompleted -= HandleFiveWhysCompleted;
        if (uiPanel != null) uiPanel.SetActive(false);
    }

    private void Update()
    {
        if (Phase != BuildPhase.Testing) return;

        testTimer += Time.deltaTime;
        foreach (var plank in placedPlanks) plank.UpdateStressVisual();

        if (cart.transform.position.y < failY || testTimer >= maxTestDuration)
        {
            HandleTestFailed();
            return;
        }

        if (goalMarker != null && cart.transform.position.x >= goalMarker.position.x)
            HandleTestSucceeded();
    }

    // ==========================================
    // DRAG PLACEMENT — called from BridgeBuilderState.Tick()
    // ==========================================

    public void HandleDragStart(Vector3 worldPos)
    {
        if (Phase != BuildPhase.Building) return;

        rawPressWorldPos = worldPos;
        dragStartNode = FindNearestNode(worldPos, nodeSnapRadius);
        dragStartPoint = dragStartNode != null ? dragStartNode.transform.position : SnapToGrid(worldPos);
        isDragging = true;
    }

    public void HandleDragUpdate(Vector3 worldPos)
    {
        if (!isDragging || previewLine == null) return;

        // A fresh press that hasn't moved yet shouldn't flash a same-point line — only show the
        // beam preview once the gesture has actually become a drag.
        if (Vector3.Distance(rawPressWorldPos, worldPos) < ClickDragThreshold)
        {
            previewLine.enabled = false;
            return;
        }

        Vector3 endPoint = ResolveDragEnd(worldPos, out _);
        previewLine.enabled = true;
        UpdatePreviewLine(dragStartPoint, endPoint);
    }

    public void HandleDragEnd(Vector3 worldPos)
    {
        if (!isDragging) return;
        isDragging = false;
        if (previewLine != null) previewLine.enabled = false;

        // A press+release with barely any movement is a click, not a placement attempt — select
        // (or deselect) the pressed-on node for the separate delete action instead.
        if (Vector3.Distance(rawPressWorldPos, worldPos) < ClickDragThreshold)
        {
            HandleNodeSelectClick(dragStartNode);
            return;
        }

        // ResolveDragEnd excludes dragStartNode from its search, so endNode can never resolve
        // back to the same node the drag started on.
        Vector3 endPoint = ResolveDragEnd(worldPos, out BridgeNode endNode);

        // At least one end must already exist — no fully-floating two-new-nodes-in-one-drag
        // placement (see class comment).
        if (dragStartNode == null && endNode == null) return;

        float length = Vector3.Distance(dragStartPoint, endPoint);
        if (length > maxPlankLength) return;

        float cost = SelectedMaterial.costPerUnitLength * length;
        if (cost > RemainingBudget) return;

        if (dragStartNode != null && endNode != null &&
            plankLookup.ContainsKey(MakeKey(dragStartNode.NodeIndex, endNode.NodeIndex)))
            return; // already connected — no duplicate plank

        Execute(new PlaceBeamAction(this, dragStartNode, dragStartPoint, endNode, endPoint, SelectedMaterial));
    }

    // Called from BridgeBuilderState.Exit() so leaving mid-drag (e.g. ESC) can't strand a
    // half-finished gesture or a stuck preview line.
    public void CancelDrag()
    {
        isDragging = false;
        if (previewLine != null) previewLine.enabled = false;
    }

    // Main Camera's Culling Mask permanently excludes both bridge layers (set once in the
    // Editor) so the playground can't leak into normal Exploration — including while this
    // container is active but the player Esc'd out mid-build without completing the mission.
    // These two flip both bits on for the duration of BridgeBuilderState only; see
    // BridgeBuilderState.Enter/Exit.
    public void ShowBridgeLayers()
    {
        if (Camera.main == null) return;
        Camera.main.cullingMask |= LayerBit(bridgeLayerName) | LayerBit(bridgeSupportLayerName);
    }

    public void HideBridgeLayers()
    {
        if (Camera.main == null) return;
        Camera.main.cullingMask &= ~(LayerBit(bridgeLayerName) | LayerBit(bridgeSupportLayerName));
    }

    private static int LayerBit(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer >= 0 ? 1 << layer : 0;
    }

    private void HandleNodeSelectClick(BridgeNode clicked)
    {
        if (selectedNode != null) selectedNode.SetSelected(false);
        selectedNode = (clicked != null && clicked != selectedNode) ? clicked : null;
        if (selectedNode != null) selectedNode.SetSelected(true);
    }

    private Vector3 ResolveDragEnd(Vector3 worldPos, out BridgeNode resolvedNode)
    {
        resolvedNode = FindNearestNode(worldPos, nodeSnapRadius, dragStartNode);
        return resolvedNode != null ? resolvedNode.transform.position : SnapToGrid(worldPos);
    }

    private BridgeNode FindNearestNode(Vector3 worldPos, float radius, BridgeNode exclude = null)
    {
        BridgeNode nearest = null;
        float nearestDist = radius;
        foreach (var node in nodes)
        {
            if (node == exclude) continue;
            float dist = Vector3.Distance(node.transform.position, worldPos);
            if (dist <= nearestDist) { nearest = node; nearestDist = dist; }
        }
        return nearest;
    }

    private Vector3 SnapToGrid(Vector3 worldPos)
    {
        float x = playgroundBounds.xMin + Mathf.Round((worldPos.x - playgroundBounds.xMin) / gridSpacing) * gridSpacing;
        float y = playgroundBounds.yMin + Mathf.Round((worldPos.y - playgroundBounds.yMin) / gridSpacing) * gridSpacing;
        x = Mathf.Clamp(x, playgroundBounds.xMin, playgroundBounds.xMax);
        y = Mathf.Clamp(y, playgroundBounds.yMin, playgroundBounds.yMax);
        return new Vector3(x, y, 0f);
    }

    private void UpdatePreviewLine(Vector3 start, Vector3 end)
    {
        previewLine.SetPosition(0, start);
        previewLine.SetPosition(1, end);
        Color color = SelectedMaterial != null ? SelectedMaterial.plankColor : Color.white;
        previewLine.startColor = color;
        previewLine.endColor = color;
    }

    // Wired to the material-picker buttons' OnClick in the Inspector, one per index into
    // Materials — see BridgeBuilderUI.
    public void SelectMaterial(int index)
    {
        if (materials == null || index < 0 || index >= materials.Length) return;
        SelectedMaterial = materials[index];
    }

    // ==========================================
    // NODE DELETION
    // ==========================================

    // Wired to the Delete button's OnClick and the Delete/Backspace key in the Inspector/
    // BridgeBuilderState — deletes whichever node HandleNodeSelectClick last selected.
    public void DeleteSelectedNode()
    {
        if (TryDeleteNode(selectedNode)) selectedNode = null;
    }

    private bool TryDeleteNode(BridgeNode node)
    {
        if (Phase != BuildPhase.Building) return false;
        if (node == null || node.IsAnchor) return false;

        Execute(new DeleteNodeAction(this, node));
        return true;
    }

    // ==========================================
    // UNDO / REDO
    // ==========================================

    private void Execute(IBridgeAction action)
    {
        action.Redo();
        undoStack.Push(action);
        redoStack.Clear();
    }

    public void Undo()
    {
        if (Phase != BuildPhase.Building || undoStack.Count == 0) return;
        IBridgeAction action = undoStack.Pop();
        action.Undo();
        redoStack.Push(action);
    }

    public void Redo()
    {
        if (Phase != BuildPhase.Building || redoStack.Count == 0) return;
        IBridgeAction action = redoStack.Pop();
        action.Redo();
        undoStack.Push(action);
    }

    // ==========================================
    // INTERNAL HELPERS — called by BridgeActions.cs as well as this class
    // ==========================================

    internal int AllocateNodeIndex() => nextNodeIndex++;

    internal BridgeNode CreateNode(Vector3 pos, int index)
    {
        BridgeNode node = Instantiate(nodePrefab, pos, Quaternion.identity, nodesParent);
        node.InitializeRuntime(index);
        nodes.Add(node);
        return node;
    }

    internal void DestroyNodeInternal(BridgeNode node)
    {
        nodes.Remove(node);
        if (selectedNode == node) selectedNode = null;
        Destroy(node.gameObject);
    }

    internal BridgeNode FindNodeByIndex(int index) => nodes.FirstOrDefault(n => n.NodeIndex == index);

    internal List<BridgePlank> FindPlanksTouching(BridgeNode node)
        => placedPlanks.Where(p => p.NodeA == node || p.NodeB == node).ToList();

    internal BridgePlank PlacePlankInternal(BridgeNode a, BridgeNode b, BridgeMaterialData material)
    {
        BridgePlank plank = Instantiate(plankPrefab, planksParent);
        plank.Setup(a, b, material);

        // Road planks stay on the prefab's authored layer (bridgeLayerName, same as the cart);
        // reinforcement-only planks move to bridgeSupportLayerName, which Project Settings >
        // Physics 2D > Layer Collision Matrix has configured to never collide with the cart —
        // see BridgeMaterialData.isRoad.
        string layerName = material.isRoad ? bridgeLayerName : bridgeSupportLayerName;
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0) plank.gameObject.layer = layer;

        placedPlanks.Add(plank);
        plankLookup[MakeKey(a.NodeIndex, b.NodeIndex)] = plank;
        budgetUsed += plank.Cost;

        AudioManager.Instance.PlaySFX(placeSfx);
        RaiseBuildProgress();
        return plank;
    }

    internal void RemovePlankInternal(BridgePlank plank)
    {
        var key = MakeKey(plank.NodeA.NodeIndex, plank.NodeB.NodeIndex);
        placedPlanks.Remove(plank);
        plankLookup.Remove(key);
        budgetUsed -= plank.Cost;
        Destroy(plank.gameObject);

        AudioManager.Instance.PlaySFX(removeSfx);
        RaiseBuildProgress();
    }

    private static (int, int) MakeKey(int a, int b) => a < b ? (a, b) : (b, a);

    private void RaiseBuildProgress()
        => EventBus.RaiseObjectiveProgress(missionID, SolutionType.Optimal, 0, Mathf.RoundToInt(budgetUsed), Mathf.RoundToInt(budget));

    // Feedback hook only — see the class comment on why a break doesn't need its own handling.
    public void NotifyPlankBroken(BridgePlank plank) => AudioManager.Instance.PlaySFX(breakSfx);

    // ==========================================
    // BUILD / TEST LIFECYCLE
    // ==========================================

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

        CancelDrag();
        if (selectedNode != null) { selectedNode.SetSelected(false); selectedNode = null; }

        foreach (var plank in placedPlanks) Destroy(plank.gameObject);
        placedPlanks.Clear();
        plankLookup.Clear();
        budgetUsed = 0f;

        undoStack.Clear();
        redoStack.Clear();

        // Deck nodes are entirely player-placed at runtime — nothing to "return to original"
        // (see BridgeNode's class comment), so a reset destroys them outright rather than
        // repositioning them. Anchors are Editor-authored and never touched.
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i].IsAnchor) continue;
            Destroy(nodes[i].gameObject);
            nodes.RemoveAt(i);
        }
        nextNodeIndex = nodes.Count > 0 ? nodes.Max(n => n.NodeIndex) + 1 : 0;

        if (cart != null && cartStartPoint != null) cart.ResetToStart(cartStartPoint.position);

        RaiseBuildProgress();
    }

    private void HandleTestFailed()
    {
        cart.StopDrive();
        attemptsUsed++;

        if (attemptsUsed >= MaxTestAttempts)
        {
            // Out of attempts: the build never held under test, so this mission resolves
            // trivially — the quick, unreinforced bridge outcome — with no separate fetch-quest
            // to play. MinigameActivator.singleContainerForMission is what lets this same
            // container close correctly on either outcome now.
            EventBus.RaiseMissionCompleted(missionID, false);
            return;
        }

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
        attemptsUsed = 0;
        ResetBridge();
    }

    private void HandleFiveWhysCompleted(int id, int correctCount)
    {
        if (id != missionID) return;
        correctWhysCount = correctCount;
    }
}
