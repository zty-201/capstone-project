# Mission 4 (The Tangled Marketplace) — Editor Setup Guide

All gameplay logic is in `Assets/Scripts/Core/Missions/Mission4/` and the new
`KanbanBuilderState`. Mission data (`M4_1.asset`) is already fully authored —
complaint, root cause, 5 Whys chain, reflection texts, directory objectives.
Mission ID **4** is already registered in `MissionRegistry` and in `Stage2`
(`StageData.missionIDs = [3, 4, 5]`) — nothing to change there. What's left
is scene/prefab wiring, which only the Editor can do.

Unlike Mission 3/5 (Advanced Missions, one container, the minigame's own
result decides `wasOptimal`), Mission 4 is a **classic** mission — the 5
Whys quiz picks the path directly via the normal `OnSolutionSelected` flow,
so it needs **two** containers and **two** `MinigameActivator`s, same shape
as Mission 1/2, not Mission 3/5's single-container shape.

## 1. The `Mission4` group (world-space)

Create a root-level `Mission4` GameObject at the marketplace map location.

## 2. The Merchant NPC (mission trigger)

Place an `NPCController` (the same component Mission 1's well uses,
`Mission1NPCInteractble.cs` — despite the filename it's the generic
NPC-dialogue-trigger component, not mission-specific) on a `Merchant_NPC`
GameObject under `Mission4`. Add `NPCPatrol` too if you want the merchant to
wander like `Farmer_NPC` does. Assign `associatedMission = M4_1`. Needs a
`Collider2D` for `InputManager`'s `Physics2D.OverlapPoint` walk-up-and-click
detection, same as every other `IInteractable`.

## 3. Trivial container (`Container_Trivial_M4`) — "Restock by Feel"

World-space, same shape as `Container_Trivial_M2` (the rubble-clearing
container) — a set of `IInteractable` pieces scattered in the world, not UI.

Create a root-level `Container_Trivial_M4` GameObject (starts **inactive**,
like every minigame container). Add `MarketStallTrivialSystem` to its root.

### Stall prop (×4 — Produce, Fish, Tools, Cloth)

Not strictly a prefab requirement (four one-off props are fine), but a
prefab keeps them in sync. Each stall needs:
- A `SpriteRenderer` (the stall's stock visual) → `MarketStall.stockVisual`.
  Its color gets lerped between `emptyColor` (default red) and `fullColor`
  (default green) as stock drains — no separate sprite swap needed, same
  cheap-visual-feedback approach `BridgePlank.UpdateStressVisual` uses for
  stress.
- A `Collider2D` (for `InputManager`'s `OverlapPoint` check, same as every
  other world `IInteractable`).
- The `MarketStall` component itself.

Place all 4 stall instances anywhere sensible under `Container_Trivial_M4`
and assign them, in the same order you'll use for the optimal container's
`stalls[]` below (Produce/Fish/Tools/Cloth), to
`MarketStallTrivialSystem.stalls`.

### `MarketStallTrivialSystem` fields

- `missionID` → 4.
- `stalls` → the 4 `MarketStall`s above.
- `dayDuration` → how long (real seconds) the simulated trivial day runs
  before it always resolves to `RaiseMissionCompleted(4, false)` regardless
  of how many stalls were sitting empty. Suggested **30s** — long enough
  that a player who's actually paying attention can keep every stall mostly
  full, but a distracted player will visibly lose one or two along the way.
- `depletionRate` (on each `MarketStall`, not the system) — suggested
  **0.08** (a stall goes from full to empty in ~12.5s if never restocked).
  Vary it slightly per stall if you want some to feel more urgent than
  others, same spirit as the optimal path's per-stall `consumptionRate`
  below — but it doesn't need to match those values exactly; the trivial
  path is deliberately "restock whatever looks empty," not a tuned puzzle.

## 4. Optimal container (`Container_Optimal_M4`) — the Kanban panel

Screen-space Canvas, same shape as `Container_Optimal_M3` — one GameObject
carrying `Canvas`/`CanvasScaler`/`GraphicRaycaster` directly, not nested
under `PlayerCanvas`, not a separate wrapper. Create a root-level
`Container_Optimal_M4`, add `Canvas` (**Screen Space - Overlay**),
`CanvasScaler` (Scale With Screen Size, matching the rest of the project's
Canvases). Starts **inactive**. Put `KanbanBuilderSystem` on this same root.

### Gauge prefab (×4 — Produce, Fish, Tools, Cloth)

A vertical "click-anywhere slider" — dragging anywhere along the bar moves
the reorder-point marker, same forgiving-hit-area idea as a normal UI
slider, rather than requiring a precise grab on a tiny pin.

```
KanbanGauge_<Stall>          Image (track background, Raycast Target ON)
                              + KanbanStallGaugeUI component
 ├─ Fill                     Image (Fill Method: Vertical, Fill Origin: Bottom)
 ├─ Marker                   Image (small horizontal bar/pin)
 └─ Label                    TextMeshProUGUI
```

Wire `KanbanStallGaugeUI`'s fields:
- `track` → the gauge root's own `RectTransform` (drag the
  `KanbanGauge_<Stall>` object onto its own `track` field — this is the
  object whose height defines the 0..1 drag range, and it's also what's
  actually catching the drag via its Raycast-Target `Image`).
- `thresholdMarker` → the `Marker` child's `RectTransform`.
- `stockFillImage` → the `Fill` child's `Image`.
- `markerColorImage` → the `Marker` child's `Image` (tints yellow live while
  dragging if you drag above the wasteful line — instant feedback before
  the player even runs the day).
- `stallNameLabel` → the `Label` child.

Instantiate 4 gauge instances as children of `Container_Optimal_M4`
(anywhere in a row — a `Horizontal Layout Group` on their shared parent
keeps them evenly spaced) and assign them, **in the same order as
`stalls[]` below**, to `KanbanBuilderSystem.gauges`.

### Status text and Run Day button

Add a `KanbanBuilderUI` component to `Container_Optimal_M4`'s root (or a
child), wire `system` → `KanbanBuilderSystem`, `statusText`, and a **Run
Day** `Button` wired to `KanbanBuilderUI.OnRunDayPressed` in the Inspector
— same "buttons call straight into the owning system" pattern as
`RoutineBuilderUI`/`BridgeBuilderUI`.

### `KanbanBuilderSystem` fields — suggested starting values

- `missionID` → 4.
- `stalls` — 4 entries, **index order must match `gauges[]` above**
  (`gauges[i]` always represents `stalls[i]`, same fixed-array-authored-in-
  parallel shape as `FarmRoutineSystem.stations`/`cards`). Suggested
  starting tuning, all at `maxStock: 10` for a consistent gauge scale:

| # | `stallName` | `consumptionRate` | `deliveryLeadTime` | `wastefulThresholdRatio` | Needed ratio\* | Healthy band |
|---|---|---|---|---|---|---|
| 0 | Produce | 1.2 | 2s | 0.55 | 0.24 | ~0.30–0.50 |
| 1 | Fish    | 0.8 | 4s | 0.65 | 0.32 | ~0.35–0.60 |
| 2 | Tools   | 0.3 | 5s | 0.75 | 0.15 | ~0.20–0.70 |
| 3 | Cloth   | 0.5 | 6s | 0.60 | 0.30 | ~0.35–0.55 |

  \*"Needed ratio" = `consumptionRate * deliveryLeadTime / maxStock` — the
  minimum reorder point that survives the delivery wait without hitting 0.
  Deliberately uneven on purpose: **Produce** is tight (fast-selling,
  quick delivery, narrow band — teaches the tension directly), **Tools**
  is forgiving (slow-selling, wide band — an easy win that still
  demonstrates the concept), **Fish**/**Cloth** sit in between. A stall
  whose columns don't leave a gap between "needed ratio" and
  `wastefulThresholdRatio` has no valid answer — double-check the two don't
  invert if you retune one.
- `simDuration` — suggested **20s** (default in code is 12s; override it in
  the Inspector). 12s is too short for Cloth's 6s lead time to complete even
  one full delivery cycle during testing; 20s gives every stall at least one
  full drain-and-refill to actually observe.
- `successSfx`/`failSfx` — optional, played once at the end of
  `RunDay()`'s simulated day.

## 5. `MinigameActivator` wiring (both paths)

Same two-activator shape as Mission 1/2 — **not** Mission 3/5's
`singleContainerForMission` shape, since Mission 4 has two real containers
and the quiz already decided which one plays.

**`Activator_Mission4_Trivial`** (a dedicated GameObject next to, not
inside, `Container_Trivial_M4` — `MinigameActivator` never lives on the
container it activates, since it subscribes to `OnSolutionSelected` in its
own `OnEnable`, which wouldn't run on an object that starts inactive):
- `missionID` → 4
- `solutionType` → **Trivial**
- `container` → `Container_Trivial_M4`
- `targetState` → **Exploration** (same as Mission 1/2's trivial paths —
  plain walk-up-and-click `IInteractable`s, no dedicated state needed)
- `singleContainerForMission` → unchecked

**`Activator_Mission4_Optimal`**:
- `missionID` → 4
- `solutionType` → **Optimal**
- `container` → `Container_Optimal_M4`
- `targetState` → **`KanbanBuilder`**
- `singleContainerForMission` → unchecked

## 6. Mission Directory HUD

Add a `MissionDirectoryUI.DirectoryEntry` for `M4_1` with its own
`TextMeshProUGUI` line, same as the existing missions. Both
`trivialObjectives`/`optimalObjectives` on `M4_1` are single-entry (no
sub-stage granularity worth tracking — same reasoning as Mission 1/2's
simple paths), so `MinigameActivator`'s own stage-0 raise right before
`container.SetActive(true)` is the only `OnObjectiveProgress` call either
path needs; neither `MarketStallTrivialSystem` nor `KanbanBuilderSystem`
raises it directly.

## 7. Tuning knobs (playtest and adjust)

- `MarketStallTrivialSystem.dayDuration` (suggested 30s) /
  `MarketStall.depletionRate` per stall (suggested 0.08) — trivial path
  pacing.
- `KanbanBuilderSystem.stalls[]` — see the table above; the whole puzzle's
  difficulty lives in these four rows. Widen a band by lowering
  `consumptionRate`/`deliveryLeadTime` or raising `wastefulThresholdRatio`;
  narrow it the other way.
- `KanbanBuilderSystem.simDuration` (suggested 20s) — longer gives more
  delivery cycles to visibly succeed or fail across, same tradeoff as
  `FarmRoutineSystem.stepDelay`/`resultHoldDuration`'s pacing knobs.

Everything downstream of `OnMissionCompleted` (reflection text, coin
reward, trust, Stage Gate redo, Mission Directory resolved state) is
already wired for free and needs no Mission-4-specific handling — those
systems only ever cared about the final `wasOptimal`, same as every other
mission.
