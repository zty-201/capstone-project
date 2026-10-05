# Mission 4 (The Tangled Marketplace) — Editor Setup Guide

All gameplay logic is in `Assets/Scripts/Core/Missions/Mission4/` and the new
`KanbanBuilderState`. Mission data (`M4_KanBanMarket.asset`) is already fully
authored — complaint, root cause, 5 Whys chain, reflection texts, directory
objectives. Mission ID **4** is already registered in `MissionRegistry` and
in `Stage2` (`StageData.missionIDs = [3, 4, 5]`) — nothing to change there.
What's left is scene/prefab wiring, which only the Editor can do.

Mission 4 is an **Advanced Mission** (`MissionData.isAdvancedMission`), same
shape as Mission 3/5 — **one** container, no separate trivial path. The 5
Whys quiz doesn't pick `SolutionType` at all: `PlanningUI.SelectAdvancedMission()`
always routes into the single Kanban panel, and `KanbanBuilderSystem`'s own
pass/fail on the simulated day decides `wasOptimal` directly. The quiz score
instead buys bonus attempts (`baseAttempts` + `bonusAttemptsPerCorrectWhy` ×
correct answers) — same "quiz score becomes practice attempts, not the
decision itself" idea `FarmRoutineSystem`/`BridgeBuilderSystem` already use.

> **If you previously built the earlier two-container version** (a
> `Container_Trivial_M4` with `MarketStallTrivialSystem` + 4 tappable world
> `MarketStall`s, plus a separate `Activator_Mission4_Trivial`): that shape
> is retired for Mission 4's Do phase. Delete (or deactivate)
> `Container_Trivial_M4` and `Activator_Mission4_Trivial` from the scene —
> `PlanningUI.SelectAdvancedMission()` never raises `SolutionType.Trivial`
> for an advanced mission, so that activator can no longer ever fire.
> **Don't delete the scripts** (`MarketStallTrivialSystem.cs`/`MarketStall.cs`)
> — they're earmarked for reuse in the post-5-missions farming/market
> sandbox later, just unplugged from Mission 4 specifically.

## 1. The `Mission4` group (world-space)

Create a root-level `Mission4` GameObject at the marketplace map location.

## 2. The Merchant NPC (mission trigger)

Place an `NPCController` (`Assets/Scripts/Core/Missions/NPCController.cs` —
the same generic dialogue-trigger component Mission 1's well uses) on a
`Merchant_NPC` GameObject under `Mission4`. Add `NPCPatrol` too if you want
the merchant to wander like `Farmer_NPC` does. Assign
`associatedMission = M4_KanBanMarket`. Needs a `Collider2D` for
`InputManager`'s `Physics2D.OverlapPoint` walk-up-and-click detection, same
as every other `IInteractable`.

## 3. The Kanban panel (`Container_Optimal_M4`) — Mission 4's only container

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

### Static instructions, status/attempts text, day progress, and the Run Day button

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
  list plus remaining attempts, or the success message).
- `attemptsText` → a `TextMeshProUGUI` showing "Attempts left: N" —
  updated every frame from `system.RemainingAttempts`, same always-visible
  pattern `RoutineBuilderUI.attemptsText` uses for Mission 3.
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
- `baseAttempts` (default 5) — matching Mission 3's "5 tries" convention
  directly.
- `bonusAttemptsPerCorrectWhy` (default 1) — same idea as
  `FarmRoutineSystem.bonusAttemptsPerCorrectWhy`/
  `BridgeBuilderSystem.bonusAttemptsPerCorrectWhy`: a strong 5 Whys
  diagnosis earns extra attempts on top of the base 5, even though this
  mission's quiz doesn't pick trivial vs. optimal either.
- `successSfx`/`failSfx` — optional, played once at the end of each
  `RunDay()`'s simulated day (a failed-but-not-yet-exhausted run still
  plays `failSfx`).

## 4. `MinigameActivator` wiring — single container, `singleContainerForMission` checked

Same shape as Mission 3/5 — **not** Mission 1/2's two-activator shape,
since there's only one real container here and the quiz never decides
which path plays.

`MinigameActivator` never lives on the container it activates (it
subscribes to `OnSolutionSelected` in its own `OnEnable`, which wouldn't
run on an object that starts inactive). Create a dedicated
`Activator_Mission4_Optimal` GameObject next to (not inside)
`Container_Optimal_M4`:
- `missionID` → 4
- `solutionType` → **Optimal**
- `container` → `Container_Optimal_M4`
- `targetState` → **`KanbanBuilder`**
- **`singleContainerForMission` → checked** — this one container can end
  in either outcome (pass the day, or exhaust attempts), and without this
  flag `MinigameActivator` only closes the container when `wasOptimal`
  happens to match its own `solutionType`, leaving it stuck open on a
  trivial (attempts-exhausted) result.

## 5. Mission Directory HUD

Add a `MissionDirectoryUI.DirectoryEntry` for `M4_KanBanMarket` with its own
`TextMeshProUGUI` line, same as the existing missions. `trivialObjectives`
is intentionally empty — same reason as Mission 3/5: the single container's
`MinigameActivator.solutionType` is always `Optimal`, so a trivial-path line
never gets raised. `KanbanBuilderSystem` raises `OnObjectiveProgress` at
stage index 0 itself (on every reset and after every failed-but-not-exhausted
run), matching `M4_KanBanMarket.optimalObjectives[0]`'s `{0}/{1}`
placeholders (attempts used / max attempts) — same pattern
`FarmRoutineSystem` uses for Mission 3's directory line.

## 6. Tuning knobs (playtest and adjust)

- `KanbanBuilderSystem.stalls[]` — see the table above; the whole puzzle's
  difficulty lives in these four rows. Widen a band by lowering
  `consumptionRate`/`deliveryLeadTime` or raising `wastefulThresholdRatio`;
  narrow it the other way.
- `KanbanBuilderSystem.simDuration` (suggested 20s) — longer gives more
  delivery cycles to visibly succeed or fail across, same tradeoff as
  `FarmRoutineSystem.stepDelay`/`resultHoldDuration`'s pacing knobs.
- `KanbanBuilderSystem.baseAttempts`/`bonusAttemptsPerCorrectWhy` — see
  above; this is the mission's actual difficulty/forgiveness dial now that
  there's no separate trivial path to fall back on.
- `MarketAmbientSystem.minRestockInterval`/`maxRestockInterval` (suggested
  8s–20s) and however many `attendants[]` you assign — see §7 below.
- `KanbanStallGaugeUI.calloutDuration` (suggested 1.5s) — how long
  "Reordered!"/"Restocked!"/"Ran dry!" stays on screen per event. Too short
  and it's unreadable during a fast simulation; too long and overlapping
  events on the same stall get cut off by `ShowEventCallout`'s own
  restart-the-coroutine behavior before the player finishes reading the
  first one.

## 7. Post-completion epilogue (`MarketAmbientSystem`) — optional, world-permanent

Entirely separate from the Do-phase container above, and not required for
the mission itself to work — this is a permanent world system that starts
running only *after* Mission 4 resolves, dramatizing which fix actually
stuck. Same trigger `RiverManager` uses for its own permanent
post-completion visual swap (`OnMissionCompleted` for missionID 4), just
driving an ongoing simulation instead of a one-time flip. It doesn't care
*how* the mission resolved trivially (there's no trivial minigame anymore —
"trivial" here just means the player ran out of attempts), only that
`wasOptimal` came back false, so nothing about this section changes from
Mission 4's earlier two-container design.

### A set of persistent world stalls (separate from the Kanban panel's gauges)

The Kanban panel's gauges are abstract UI, not world objects. The epilogue
needs its own 4 `MarketStall` instances (`Assets/Scripts/Core/Missions/Mission4/MarketStall.cs`
— reused from the retired trivial path, see the note at the top of this
doc), placed permanently in the always-visible marketplace.

**Do not add a `Collider2D`** to these. `MarketStall` still implements
`IInteractable`, but without a collider `InputManager`'s
`Physics2D.OverlapPoint` simply can't detect them, which is what makes them
purely decorative/non-clickable — the player watches this epilogue, they
don't operate it.

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
(not inside `Container_Optimal_M4`):
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
