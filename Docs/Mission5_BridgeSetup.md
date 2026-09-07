# Mission 5 (Bridge Building) — Editor Setup Guide

All gameplay logic is in `Assets/Scripts/Core/Missions/Mission3/` and the new
`BridgeBuilderState`. Mission data (`M5_BrokenBridge.asset`) is already
authored. What's left is scene wiring, which only the Editor can do.

The bridge is presented as a popup, same idea as the pipe puzzle's
`Container_Optimal_M1` (always framed the same way regardless of where the
player triggered it) — but reached the opposite way round. The pipe puzzle's
container can carry a `CameraFollower` (re-centers itself on the camera every
frame) because it has zero `Rigidbody2D` anywhere; `Container_Optimal_M5` is
nothing *but* `Rigidbody2D`-driven objects (nodes, planks, the cart), and
Unity does not carry a moving non-physics parent's motion into a `Rigidbody2D`
child — the child's transform gets corrected back to hold its world position,
so a `CameraFollower` here would leave the physics playground stuck in place
while only a background sprite moved. So `Container_Optimal_M5` stays exactly
where it's authored (no relocation, ever) and instead **a second Cinemachine
camera moves to frame it** — see "Popup camera" in section 3 below.

This is a full Poly Bridge: free node placement via drag (only anchors are
still Editor-authored — see "Node grid — anchors only" in section 3), a
choice of `BridgeMaterialData` materials (cost vs. strength), stress-under-load
color feedback during the test, and undo/redo — not the earlier fixed-grid,
click-two-existing-nodes version.

Mission ID **5** is already registered in `MissionRegistry` and in `Stage2`
(`StageData.missionIDs = [3, 4, 5]`) — nothing to change there.

## 1. The broken bridge itself

Place a `BridgeInteractable` on the bridge GameObject in the world (same role as
`RiverInteractable` on the boulder). Assign `associatedMission = M5_BrokenBridge`.

Add a `BridgeManager` somewhere persistent in the scene (not inside either
container) with three visuals wired in:
- `brokenBridgeVisual` — the current broken-bridge sprite (active by default).
- `lashedBridgeVisual` — a rickety wooden-plank-crossing sprite (inactive by default).
- `bracedBridgeVisual` — a finished, properly-braced bridge sprite (inactive by default).

### Unlocking the far bank

Both paths physically get the player across, so both should unlock it. Paint
the far-bank area into the map now (visible from the start is a nice touch —
"I can see it, I just can't reach it yet"), but mark the bridge span/entrance
cells **unwalkable** in `PathfindingSystem`'s `collisionTilemap` so the player
can't path there before the mission resolves.

Wire `BridgeManager.pathfindingSystem` to the scene's `PathfindingSystem`, and
fill `unlockedCells` with the world position of every one of those cells (the
bridge span + the landing spot on the far side is usually enough — pathing
does the rest once those cells are walkable). This unlock is intentionally
one-way: a trivial outcome flagged for a Stage Gate redo does **not** re-lock
the cells, since the player could already be exploring the far bank when that
happens, and re-locking their only way back would strand them there.

## 2. No separate trivial container

Advanced missions (`MissionData.isAdvancedMission`) don't route to a distinct
trivial-path minigame — there's only one minigame, and its own simulation
result decides trivial vs. optimal (see `PlanningUI.SelectAdvancedMission` /
`BridgeBuilderSystem.HandleTestFailed`). `PlankPickup`/`BridgePlankPoint` and
`Plank.asset` no longer exist (deleted). If your `Mission5` group still has
the earlier trivial-path scaffolding under it, delete **both**:
- `Container_Trivial_M5` — the container itself.
- `Activator_Mission5_Trivial` — its `MinigameActivator`, a **separate**
  sibling GameObject, not a component on the container (see the note on
  `MinigameActivator` placement below).

## 3. The minigame container (`Container_Optimal_M5`)

Only one container this mission.

**`MinigameActivator` never lives on the container it activates.** It
subscribes to `EventBus.OnSolutionSelected` in its own `OnEnable` — if it were
a component on `Container_Optimal_M5`, which starts inactive, that `OnEnable`
would never run until the container is already active, which is exactly what
`OnSolutionSelected` is supposed to trigger in the first place. That's why
every mission's activators are their own dedicated `Activator_Mission{N}_*`
GameObjects, not children of the container, wired to it only via the
`container` field. Mission 5 already has one such object,
`Activator_Mission5_Optimal`, sitting alongside `Container_Optimal_M5` under
the `Mission5` group — configure it, don't add a new one:
- `missionID` → 5
- `container` → `Container_Optimal_M5`
- `targetState` → **`BridgeBuilder`** (check this — it's easy to leave at the
  default `Exploration`)
- **`singleContainerForMission` → checked.** This one container can end in
  *either* outcome now; without this flag, `MinigameActivator` only closes
  the container when `wasOptimal` happens to match its `solutionType`,
  leaving it (and the UI panel) stuck open on a trivial result.

**`Container_Optimal_M5` itself must start inactive** — like every other
minigame container, it should only appear once `OnSolutionSelected` fires.
Double check its active checkbox; it's easy to leave checked while you're
building it out in the Editor and forget to switch off before playtesting.

Put `BridgeBuilderSystem` on the container's own root —
`GetComponentsInChildren<BridgeNode>` requires every node to be a child of
this GameObject.

### Layer (hides the playground when it's active but not being viewed)

`Container_Optimal_M5` stays active for the entire build/test session — it's
only deactivated once the mission actually completes. That's a gap: pressing
Esc mid-build (Building phase only — see `BridgeBuilderState`) returns to
Exploration *without* completing the mission, so the container is still
active, still sitting at its authored map location, with nothing stopping
Main Camera from rendering it if the player walks back near the bridge.

Fix: put everything under `Container_Optimal_M5` — anchors, `nodePrefab`,
`plankPrefab`, the test cart, `previewLine`, and the optional background
sprite — on a dedicated layer, and **exclude that layer from Main Camera's
Culling Mask by default** (that's the baseline for normal Exploration — see
below for why "default", not "always"). Use layer slot **`6`** (Project
Settings → Tags and Layers) and name it **`Bridge`**: Unity reserves indices
0-7 as "built-in" slots, but only some of them are actually
lockable-vs-editable — `0 Default`, `1 TransparentFX`, `2 Ignore Raycast`,
`4 Water`, `5 UI` are fixed names you can't rename in that window at all
(regardless of whether the engine attaches real behavior to the name — it
doesn't, for `Water` specifically), while `3` and `6`/`7` are genuinely blank
and editable within that same reserved range — which is exactly why `NPC`
could be added at index 3 earlier in this project. `6` is confirmed unused by
anything in the scene (same check as before). Since `nodePrefab`/`plankPrefab`
are instantiated at runtime, the layer has to be set on those **prefab
assets** themselves, not just on in-scene objects. `MinimapCamera` should
also permanently exclude `Bridge` — the minimap never needs to show the
playground, in any state, so it doesn't need the runtime toggle below.

**Important:** unlike a normal second camera (e.g. `MinimapCamera`), a
`CinemachineCamera` (`bridgeViewCamera`) has **no Culling Mask of its own** —
Cinemachine 3 only blends position/rotation/lens into the one real `Camera`
(Main Camera, via `CinemachineBrain`), never the Culling Mask. So a purely
static Editor exclusion would hide the `Bridge` layer permanently, *including*
while `bridgeViewCamera` is supposed to be showing it. `BridgeBuilderSystem`
handles this with `ShowBridgeLayer()`/`HideBridgeLayer()` (toggling the bit
directly on `Camera.main.cullingMask`), called from `BridgeBuilderState.Enter()`/
`Exit()` alongside the camera-active swap — no further wiring needed beyond
setting `bridgeLayerName` (default `"Bridge"`, matching the layer name above)
on `BridgeBuilderSystem` and doing the static Main-Camera exclusion described
above as the *resting* state.

### Node grid — anchors only

Free placement means **only the anchor nodes are still Editor-authored** —
every deck node is created at runtime by a drag (see `HandleDragStart`/
`HandleDragEnd`), so you no longer hand-place a fixed grid of deck nodes.

For each anchor (the two solid riverbank edges), add a child GameObject (on
the `Bridge` layer — see above) with:
- A `SpriteRenderer` (assign to `BridgeNode.visual` for the select-highlight).
- A `BridgeNode` component: check `isAnchor`, set a unique `nodeIndex` (0, 1,
  …). No `Rigidbody2D` needed — anchors never move.

No `Collider2D` needed on nodes anymore — node hit-testing is centralized in
`BridgeBuilderSystem.FindNearestNode` (a plain distance check over its own
tracked node list), not per-node collider overlap, since resolving "nearest
point within radius, existing node **or empty grid space**" isn't something
any single node's own collider could ever answer about itself.

Lay out at least two anchors (start bank, end bank) reasonably far apart —
the player builds everything else (deck nodes, the whole truss) themselves
within `gridSpacing`/`maxPlankLength`/`budget` (see Placement fields below).

### Node prefab (deck nodes — instantiated at runtime)

Create a prefab (on the `Bridge` layer — see above; set on the prefab asset
itself, since it's instantiated at runtime) — this is what
`BridgeBuilderSystem.CreateNode` instantiates every time a drag places a new
joint — with:
- A `SpriteRenderer` (assign to `BridgeNode.visual`).
- A `Rigidbody2D` — **Body Type: Kinematic**, gravity scale doesn't matter
  (ignored while Kinematic). Give it a small mass (e.g. 0.2) for when it
  switches to Dynamic during the test.
- A `BridgeNode` component with `isAnchor` **unchecked** (the prefab default —
  `InitializeRuntime` only ever sets `nodeIndex`, never touches `isAnchor`).

Assign this prefab to `BridgeBuilderSystem.nodePrefab`, and add an empty
`NodesParent` child (identity position/rotation/scale) for `nodesParent`.

### Plank prefab
Create a prefab (on the `Bridge` layer — see above; set on the prefab asset
itself, since it's instantiated at runtime) with:
- `SpriteRenderer` — a plank/beam sprite exactly **1 unit wide** at scale 1
  (pivot centered), so `BridgePlank.plankLength = 1` stretches it correctly.
  If your sprite is a different authored width, set `plankLength` to match.
  Assign this to `BridgePlank.visual` too — it's what gets tinted per
  material and color-shifted under stress (see Materials below).
- `BoxCollider2D` sized to the sprite (this is what the test cart drives on).
- `Rigidbody2D` — Body Type doesn't matter here, `BridgePlank.Setup` forces it
  to Kinematic on placement.
- `BridgePlank` component. Set `breakingColor` (default red) — the color
  every material's plank shifts toward as it nears its `breakForce` during
  the test.

Do **not** add `HingeJoint2D` to the prefab — `BridgePlank.Setup` adds and
wires both of them at runtime.

Assign this prefab to `BridgeBuilderSystem.plankPrefab`, and add an empty
`PlanksParent` child (identity position/rotation/**scale (1,1,1)**) for
`planksParent`.

### Materials

Create 2-3 `BridgeMaterialData` assets (`Kaizen Systems/Bridge Material Data`)
— e.g. **Wood** (low `costPerUnitLength`, low `breakForce`), **Steel**
(mid/mid), **Cable** (high cost, high `breakForce`) — each with a distinct
`plankColor` so placed beams read as different materials at a glance. Assign
the array to `BridgeBuilderSystem.materials`; index 0 is the default selected
material on activation.

### Placement fields

On `BridgeBuilderSystem`:
- `budget` — total currency the player spends on beam length × material
  `costPerUnitLength`. Replaces the old fixed plank-count budget.
- `maxPlankLength` — still a hard physical cap on any single beam,
  independent of material/cost.
- `gridSpacing` — snap increment for placing a new node in empty space.
- `playgroundBounds` — a `Rect` (in this container's local/world space, since
  the container itself never moves) clamping how far from the anchors the
  player can snap-place a new node.
- `nodeSnapRadius` — how close a press/release point must be to an existing
  node to resolve onto it instead of empty grid space.
- `previewLine` — a `LineRenderer` child (on the `Bridge` layer — see above;
  2 positions, `Use World Space` checked) showing the beam-in-progress while
  dragging, tinted to the selected material's color. Starts disabled.

### Test cart
A small GameObject (on the `Bridge` layer — see above) with `SpriteRenderer`,
`Rigidbody2D` (Dynamic — this is the one exception, cart starts Kinematic via
script but the component default doesn't matter), a
`BoxCollider2D`/`CircleCollider2D` for wheels-on-planks contact, and
`BridgeTestCart`. Place it parked at the near bank.

Wire on `BridgeBuilderSystem`:
- `cart` → this object.
- `cartStartPoint` → an empty Transform at its parked position.
- `goalMarker` → an empty Transform at/past the far bank — the cart "wins" once
  its X position reaches this.
- `failY` → a Y value comfortably below the lowest plank (the gorge floor) —
  the cart "loses" once it drops below this. Stays a plain absolute-world
  constant precisely because `Container_Optimal_M5` never moves (see the
  popup camera note above) — no relative/local-space math needed.

### Background sprite (optional, purely visual)

For the same "popup panel" look the pipe puzzle gets from its `Setting menu_1`
background, add a plain `SpriteRenderer` child of `Container_Optimal_M5` (on
the `Bridge` layer — see above; no `CameraFollower` — the container is static
here) sized to frame the whole node grid, sorting-ordered behind the
nodes/planks/cart.

### Popup camera (a second Cinemachine camera, not a moved container)

The scene already has one `CinemachineCamera` (on `CameraController`,
`CinemachineFollow`-tracking the player) driving `Main Camera` via
`CinemachineBrain`. Add a **second** `CinemachineCamera` GameObject (no
`CinemachineFollow`/Tracking Target — a fixed shot), positioned/sized to
frame `Container_Optimal_M5` head-on at its normal, unmoving map location.
Leave both GameObjects **active** in the hierarchy at design time —
`BridgeBuilderState` toggles which one is active at runtime, and
`CinemachineBrain` always blends to whichever one is active (exactly one at a
time, so there's no priority number to tune). It has **no Culling Mask of its
own to set** — `CinemachineCamera` doesn't carry one at all (see "Layer"
above) — visibility is handled separately, at runtime, via
`BridgeBuilderSystem.ShowBridgeLayer()`/`HideBridgeLayer()`.

Wire on `BridgeBuilderSystem`:
- `playerCamera` → the existing `CameraController` GameObject.
- `bridgeViewCamera` → the new camera GameObject.
- `bridgeLayerName` → `"Bridge"` (already the default; only change it if you
  named the layer above something else).

`BridgeBuilderState.Enter()`/`Exit()` already do the swap — no further wiring
needed. `BridgeBuilderState.Tick()` reads the pointer via
`PointerInput.TryGetPrimaryWorldPosition()`/`PrimaryPressedThisFrame()`/
`PrimaryHeld()`/`PrimaryReleasedThisFrame()`, all of which resolve through
`Camera.main` — reflecting whichever Cinemachine camera is currently driving
the brain, so drag/node resolution needs no camera-specific handling either.

### UI panel
Build a small Canvas panel (can live under the main Canvas, doesn't need to be
world-space — see `InfoBoard`/`PlanningUI` panels for the pattern) with:
- A budget `TextMeshProUGUI`.
- A status `TextMeshProUGUI`.
- **Test**, **Reset**, **Delete**, **Undo**, and **Redo** `Button`s.
- One `Button`+`TextMeshProUGUI` label pair per entry in
  `BridgeBuilderSystem.materials` (a material picker row) — matches
  `PlanningUI`'s fixed-array-of-choice-buttons shape.

Add `BridgeBuilderUI` to the panel root, wire `system` to the
`BridgeBuilderSystem` above, wire the text fields, the five buttons, and the
`materialButtons` array (one entry per material, in the same order as
`BridgeBuilderSystem.materials`). Wire each button's `OnClick` in the
Inspector: `OnTestPressed`/`OnResetPressed`/`OnDeletePressed`/`OnUndoPressed`/
`OnRedoPressed` (same pattern as `DayCompleteUI`/`InfoBoardUI`) — the material
buttons wire themselves up in code (`BridgeBuilderUI.Start()`), nothing to
wire on those in the Inspector beyond the button/label references themselves.

**Keyboard shortcuts** (no Editor wiring needed — already in
`BridgeBuilderState.Tick()`): Delete/Backspace deletes the selected node,
Ctrl+Z undoes, Ctrl+Y redoes.

Assign this panel's root GameObject to `BridgeBuilderSystem.uiPanel` — it's
shown/hidden automatically alongside the container.

## 4. Mission Directory HUD

Add a `MissionDirectoryUI.DirectoryEntry` for `M5_BrokenBridge` with its own
`TextMeshProUGUI` line, same as the existing two missions.

## 5. Tuning knobs (playtest and adjust)

All on `BridgeBuilderSystem`:
- `budget` (default 20) — raise/lower to control how hard the truss is; this
  is spent on beam length × the selected material's `costPerUnitLength`.
- `maxPlankLength` (default 3) — caps how far apart two connected points can
  be, regardless of material.
- `gridSpacing` (default 0.5) — snap increment for empty-space node placement;
  smaller reads as more freeform, larger reads more like discrete "slots".
- `nodeSnapRadius` (default 0.3) — how forgiving snapping onto an existing
  node is; too large makes precise short beams hard to place, too small makes
  it hard to reconnect to an existing joint.
- `playgroundBounds` — must comfortably contain every anchor plus however far
  out you want players able to build; nothing can snap-place outside it.
- `maxTestDuration` (default 20s) — safety timeout if the cart gets stuck.
- `baseTestAttempts` (default 3) — how many tries the player gets before a
  failed test locks in the trivial outcome.
- `bonusAttemptsPerCorrectWhy` (default 1) — extra attempts per correct answer
  in the 5 Whys quiz (0-5 correct → 0-5 bonus attempts on top of the base).

Per material (`BridgeMaterialData` asset), not on `BridgeBuilderSystem`:
- `costPerUnitLength` / `breakForce` — the actual cost-vs-strength tradeoff;
  keep at least one material where `budget / costPerUnitLength` comfortably
  clears `maxPlankLength` several times over, or the player can't build a
  full span at all.

On `BridgeTestCart`: `driveSpeed` (default 2) — faster puts more dynamic load
on the joints as it crosses.

Everything downstream of `OnMissionCompleted` (reflection text, coin reward,
trust, stage-gate redo, Mission Directory resolved state) is already wired
for free and needs no advanced-mission-specific handling —
those systems only ever cared about the final `wasOptimal`, never how it was
reached. The 5 Whys quiz itself still runs for `M5_BrokenBridge` exactly as
authored, but only feeds `bonusAttemptsPerCorrectWhy` above — it no longer
picks the path (see `PlanningUI.SelectAdvancedMission`).
