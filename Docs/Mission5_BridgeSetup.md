# Mission 5 (Bridge Building) — Editor Setup Guide

All gameplay logic is in `Assets/Scripts/Core/Missions/Mission3/` and the new
`BridgeBuilderState`. Mission data (`M5_BrokenBridge.asset`) is already
authored. What's left is scene wiring, which only the Editor can do.

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

### Node grid
For each node in your span, add a child GameObject with:
- A `CircleCollider2D` set to **Is Trigger** (click hit-testing).
- A `SpriteRenderer` (assign to `BridgeNode.visual` for the select-highlight).
- A `BridgeNode` component. Set a unique `nodeIndex` per node (0, 1, 2, …).
  - **Anchor nodes** (the two solid riverbank edges): check `isAnchor`. No
    `Rigidbody2D` needed.
  - **Deck nodes** (everything mid-span): leave `isAnchor` unchecked and add a
    `Rigidbody2D` — **Body Type: Kinematic**, gravity scale doesn't matter (it's
    ignored while Kinematic). Give it a small mass (e.g. 0.2) for when it
    switches to Dynamic during the test.

Lay out at least two anchors (start bank, end bank) and enough deck nodes
between/around them that a triangulated truss is actually possible — a single
row of collinear nodes can only ever produce a floppy one-plank-thick span.

### Plank prefab
Create a prefab with:
- `SpriteRenderer` — a plank/beam sprite exactly **1 unit wide** at scale 1
  (pivot centered), so `BridgePlank.plankLength = 1` stretches it correctly.
  If your sprite is a different authored width, set `plankLength` to match.
- `BoxCollider2D` sized to the sprite (this is what the test cart drives on).
- `Rigidbody2D` — Body Type doesn't matter here, `BridgePlank.Setup` forces it
  to Kinematic on placement.
- `BridgePlank` component.

Do **not** add `HingeJoint2D` to the prefab — `BridgePlank.Setup` adds and
wires both of them at runtime.

Assign this prefab to `BridgeBuilderSystem.plankPrefab`, and add an empty
`PlanksParent` child (identity position/rotation/**scale (1,1,1)**) for
`planksParent`.

### Test cart
A small GameObject with `SpriteRenderer`, `Rigidbody2D` (Dynamic — this is the
one exception, cart starts Kinematic via script but the component default
doesn't matter), a `BoxCollider2D`/`CircleCollider2D` for wheels-on-planks
contact, and `BridgeTestCart`. Place it parked at the near bank.

Wire on `BridgeBuilderSystem`:
- `cart` → this object.
- `cartStartPoint` → an empty Transform at its parked position.
- `goalMarker` → an empty Transform at/past the far bank — the cart "wins" once
  its X position reaches this.
- `failY` → a Y value comfortably below the lowest plank (the gorge floor) —
  the cart "loses" once it drops below this.

### UI panel
Build a small Canvas panel (can live under the main Canvas, doesn't need to be
world-space — see `InfoBoard`/`PlanningUI` panels for the pattern) with:
- A plank-count `TextMeshProUGUI`.
- A status `TextMeshProUGUI`.
- **Test** and **Reset** `Button`s.

Add `BridgeBuilderUI` to the panel root, wire `system` to the
`BridgeBuilderSystem` above, wire the two text fields and two buttons. Wire the
buttons' `OnClick` to `BridgeBuilderUI.OnTestPressed` / `OnResetPressed` in the
Inspector (same pattern as `DayCompleteUI`/`InfoBoardUI`).

Assign this panel's root GameObject to `BridgeBuilderSystem.uiPanel` — it's
shown/hidden automatically alongside the container.

## 4. Mission Directory HUD

Add a `MissionDirectoryUI.DirectoryEntry` for `M5_BrokenBridge` with its own
`TextMeshProUGUI` line, same as the existing two missions.

## 5. Tuning knobs (playtest and adjust)

All on `BridgeBuilderSystem`:
- `plankBudget` (default 8) — raise/lower to control how hard the truss is.
- `maxPlankLength` (default 3) — caps how far apart two nodes can be and still connect.
- `plankBreakForce` (default 40) — lower = bridges snap more easily under the
  cart's weight; this is the main "did you actually brace it" knob.
- `maxTestDuration` (default 20s) — safety timeout if the cart gets stuck.
- `baseTestAttempts` (default 3) — how many tries the player gets before a
  failed test locks in the trivial outcome.
- `bonusAttemptsPerCorrectWhy` (default 1) — extra attempts per correct answer
  in the 5 Whys quiz (0-5 correct → 0-5 bonus attempts on top of the base).

On `BridgeTestCart`: `driveSpeed` (default 2) — faster puts more dynamic load
on the joints as it crosses.

Everything downstream of `OnMissionCompleted` (reflection text, coin reward,
trust, stage-gate redo, Mission Directory resolved state) is already wired
for free and needs no advanced-mission-specific handling —
those systems only ever cared about the final `wasOptimal`, never how it was
reached. The 5 Whys quiz itself still runs for `M5_BrokenBridge` exactly as
authored, but only feeds `bonusAttemptsPerCorrectWhy` above — it no longer
picks the path (see `PlanningUI.SelectAdvancedMission`).
