# Mission 5 (Bridge Building) — Editor Setup Guide

All gameplay logic is in `Assets/Scripts/Core/Missions/Mission3/` and the new
`BridgeBuilderState`. Mission data (`M5_BrokenBridge.asset`) and the `Rope` item
are already authored. What's left is scene wiring, which only the Editor can do.

Mission ID **5** is already registered in `MissionRegistry` and in `Stage2`
(`StageData.missionIDs = [3, 4, 5]`) — nothing to change there.

## 1. The broken bridge itself

Place a `BridgeInteractable` on the bridge GameObject in the world (same role as
`RiverInteractable` on the boulder). Assign `associatedMission = M5_BrokenBridge`.

Add a `BridgeManager` somewhere persistent in the scene (not inside either
container) with three visuals wired in:
- `brokenBridgeVisual` — the current broken-bridge sprite (active by default).
- `lashedBridgeVisual` — a rickety rope-crossing sprite (inactive by default).
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

### Consequence for the trivial path: a stranded villager

Add a `StrandedVillager` object, positioned in the water near the far end of
the rope bridge, **inactive by default**. It reveals itself automatically the
moment Mission 5 resolves *trivially* (via `OnMissionCompleted`, so no extra
wiring needed beyond placing it and filling in `rescueDialogue`), and
disappears for good once the player interacts with it — it's a one-time
narrative beat, not something that resets on a review redo. It doesn't gate
Town Hall submission or anything else; it's flavor reinforcing the "quick fix
isn't final" theme already in `M5_BrokenBridge`'s trivial reflection text.

## 2. Trivial container (`Container_Trivial_M5`)

Same shape as `Container_Trivial_M1`. Starts inactive; a `MinigameActivator`
(missionID 5, solutionType Trivial, container = this, targetState = Exploration)
activates it on `OnSolutionSelected`.

Children:
- **RopePickup** — an `IInteractable` placed elsewhere on the map, `ropeItem = Rope.asset`.
- **BridgeLashPoint** — at the bridge, `ropeItem = Rope.asset`, `ropePickup` wired to the RopePickup above.

## 3. Optimal container (`Container_Optimal_M5`)

Same activation pattern, `targetState = BridgeBuilder`. Put `BridgeBuilderSystem`
on this container's root — `GetComponentsInChildren<BridgeNode>` requires every
node to be a child of this GameObject.

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

On `BridgeTestCart`: `driveSpeed` (default 2) — faster puts more dynamic load
on the joints as it crosses.

Everything else (5 Whys quiz, reflection text, coin reward, trust, stage-gate
redo) is already wired for free — those systems are generic over `missionID`
and just need `M5_BrokenBridge`'s data, which is already filled in.
