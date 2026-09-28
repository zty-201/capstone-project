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

Place an `NPCController` (`Assets/Scripts/Core/Missions/NPCController.cs` —
the same generic dialogue-trigger component Mission 1's well uses) on a
`Merchant_NPC` GameObject under `Mission4`. Add `NPCPatrol` too if you want
the merchant to wander like `Farmer_NPC` does. Assign `associatedMission = M4_1`. Needs a
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

Also carries three legibility additions that came out of an early
playtest: a first-time player looking at 4 bare bars had no idea what they
represented, the simulation read as silent bar-jiggling with no narration,
and a failed run gave no way to tell which stall failed or which direction
to adjust. Live percentage readouts, a per-stall event callout, and a
dedicated failure outline are the fix — see below.

```
KanbanGauge_<Stall>          Image (track background, Raycast Target ON)
                              + KanbanStallGaugeUI component
 ├─ Fill                     Image (Fill Method: Vertical, Fill Origin: Bottom)
 ├─ Marker                   Image (small horizontal bar/pin)
 ├─ Label                    TextMeshProUGUI (stall name, e.g. "Produce")
 ├─ ThresholdReadout         TextMeshProUGUI (e.g. "Reorder at 42%")
 ├─ StockReadout             TextMeshProUGUI (e.g. "78%", live during a run)
 ├─ EventCallout             TextMeshProUGUI (e.g. "Reordered!" — starts inactive)
 └─ FailureOutline           Image or border sprite (starts inactive)
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
- `thresholdReadout` → the `ThresholdReadout` child. Updates live every time
  the marker moves (`SetThresholdRatio`), both while dragging and when the
  Stage Gate system resets it — a number is much easier to judge precisely
  than bar height alone.
- `stockReadout` → the `StockReadout` child. Updates every simulated frame
  alongside the fill (`SetStockRatio`) — sits at "100%" at rest, since
  that's the true starting stock, not just a placeholder.
- `eventCalloutText` → the `EventCallout` child. **Start this object
  inactive** — `Initialize()`/`ShowEventCallout()` handle activating and
  hiding it, so leaving it active by default would show stale/empty text
  before the first event fires.
- `failureOutline` → the `FailureOutline` child (a colored border `Image`
  behind or around the bar works well). **Start this object inactive too**
  — same reasoning, `SetFailureOutline(bool)` is the only thing that
  toggles it, driven once at the end of each run.

Instantiate 4 gauge instances as children of `Container_Optimal_M4`
(anywhere in a row — a `Horizontal Layout Group` on their shared parent
keeps them evenly spaced) and assign them, **in the same order as
`stalls[]` below**, to `KanbanBuilderSystem.gauges`.

**Why a stall sitting at 90% full can still show its `FailureOutline`:**
`SetStockRatio` only ever shows the *honest* live stock color/fill —
it no longer force-colors a stall red just because it's flagged failed.
A real stockout still reads as red on its own (the color naturally
approaches `dangerColor` as stock nears 0), but a "wasteful" failure
(threshold set too high) can leave the fill looking perfectly healthy the
whole run — `FailureOutline` is the one visual that's guaranteed to show up
either way, precisely because fill color alone can't distinguish "about to
run dry" from "carrying too much stock on purpose."

### Static instructions, status text, day progress, and the Run Day button

Add a plain, always-visible `TextMeshProUGUI` somewhere near the top of
`Container_Optimal_M4` with static authored text explaining the mechanic in
plain language, e.g.: *"Each bar is a stall's stock. Drag the pin to set
when it should reorder — too low risks running dry, too high wastes
stock."* This never changes at runtime, so it isn't wired to any script —
just author it directly in the Editor, same as any other static UI label
(a button's own text, for instance).

Add a `KanbanBuilderUI` component to `Container_Optimal_M4`'s root (or a
child), wire:
- `system` → `KanbanBuilderSystem`.
- `statusText` → a `TextMeshProUGUI` for the dynamic status line (this is
  the one that changes — "Set a reorder point...", the per-stall failure
  list, or the success message).
- `runDayButton` → a **Run Day** `Button`, wired to
  `KanbanBuilderUI.OnRunDayPressed` in the Inspector — same "buttons call
  straight into the owning system" pattern as `RoutineBuilderUI`/
  `BridgeBuilderUI`.
- `dayProgressText` → optional, a `TextMeshProUGUI` showing "Day: 60%"
  while a run is in flight. `KanbanBuilderUI` handles showing/hiding it
  automatically (`system.IsSimulating`) — it disappears entirely outside a
  run rather than sitting at a stale "Day: 0%".

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
- `MarketAmbientSystem.minRestockInterval`/`maxRestockInterval` (suggested
  8s–20s) and however many `attendants[]` you assign — see §8 below.
- `KanbanStallGaugeUI.calloutDuration` (suggested 1.5s) — how long
  "Reordered!"/"Restocked!"/"Ran dry!" stays on screen per event. Too short
  and it's unreadable during a fast simulation; too long and overlapping
  events on the same stall get cut off by `ShowEventCallout`'s own
  restart-the-coroutine behavior before the player finishes reading the
  first one.

## 8. Post-completion epilogue (`MarketAmbientSystem`) — optional, world-permanent

Entirely separate from the two Do-phase containers above, and not required
for the mission itself to work — this is a permanent world system that
starts running only *after* Mission 4 resolves, dramatizing which fix
actually stuck. Same trigger `RiverManager` uses for its own permanent
post-completion visual swap (`OnMissionCompleted` for missionID 4), just
driving an ongoing simulation instead of a one-time flip.

### A third set of stalls (not the trivial minigame's)

`Container_Trivial_M4`'s 4 `MarketStall`s are temporary — they get
deactivated forever once the mission resolves, same as any other minigame
container. The epilogue needs its **own** 4 `MarketStall` instances, placed
permanently in the always-visible marketplace (visible before, during, and
after the mission plays out) — reuse the exact same `MarketStall` component
and prefab from §3.

**Do not add a `Collider2D`** to these ones. `MarketStall` still implements
`IInteractable`, but without a collider `InputManager`'s
`Physics2D.OverlapPoint` simply can't detect them, which is what makes them
purely decorative/non-clickable — the player watches this epilogue, they
don't operate it. (Contrast with §3's trivial-minigame stalls, which *do*
need a `Collider2D` since the player has to be able to tap those.)

### Attendant NPCs — as many as you want to experiment with

Build a `MarketAttendantNPC` prefab: `Animator`/`SpriteRenderer` (both
optional — null-checked, but needed for movement animation/flipping to
actually show), and a `pathfindingSystem` field wired to the scene's
`PathfindingSystem` directly (same as `NPCPatrol.pathfindingSystem` — not a
singleton lookup). Place however many instances you want in the
marketplace and drag them all into `MarketAmbientSystem.attendants[]` —
the dispatch logic round-robins pending restock jobs across whichever
attendants are currently free, so trying 1 vs. 4 attendants is purely an
Inspector-array-length experiment, no code changes needed either way.

### `MarketAmbientSystem` fields

Add this component to a permanent GameObject in the `Mission4` world group
(not inside either Do-phase container):
- `missionID` → 4.
- `stalls` → the 4 persistent stalls above, **in the same index order as
  `KanbanBuilderSystem.stalls`/`gauges`** — `GetThresholdRatio(i)` assumes
  matching indices, so stall 0 here must be the same stall (Produce) as
  index 0 in the Kanban panel.
- `attendants` → however many `MarketAttendantNPC`s you built.
- `minRestockInterval`/`maxRestockInterval` (suggested 8s/20s) — only used
  by the trivial "Unmanaged" mode's random dispatch timer; the optimal
  "Kanban" mode ignores these entirely and dispatches purely off threshold
  crossings.

### What it actually does

On `OnMissionCompleted(4, wasOptimal)`, it locks into one mode forever:
- **Trivial → "Unmanaged"**: every 8–20s, dispatches an attendant to a
  **random** stall regardless of that stall's actual stock — restocking
  has no relationship to real need, same "nobody's watching the threshold"
  idea the reflection text already states, just made literal.
- **Optimal → "Kanban"**: continuously reads each stall's live
  `MarketStall.StockRatio` against `KanbanBuilderSystem.GetThresholdRatio(i)`
  — the exact value the player actually dragged into place, not a
  re-authored ideal — and dispatches the instant it crosses. The walk
  itself stands in for delivery lead time; there's no separate abstract
  timer for it.

Everything downstream of `OnMissionCompleted` that already existed before
this feature (reflection text, coin reward, trust, Stage Gate redo, Mission
Directory resolved state) is still wired for free and needs no
Mission-4-specific handling. `MarketAmbientSystem` is the one addition that
*does* need its own manual scene wiring above — it isn't automatic the way
those other systems are.
