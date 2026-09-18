# Mission 3 (The Farmer's Broken Routine) — Editor Setup Guide

All gameplay logic is in `Assets/Scripts/Core/Missions/Mission3/` and the new
`RoutineBuilderState`. Mission data (`M3_BrokenRoutine.asset`) is already
authored. What's left is scene/prefab wiring, which only the Editor can do.

**If you already built `Container_Optimal_M3` as a world-space container
with a `CameraFollower`** (from an earlier pass at this guide): that needs
rebuilding as a Canvas panel per this version instead — the card/slot
scripts changed from `SpriteRenderer`/`Collider2D` (`RoutineCardWorld`/
`RoutineSlotWorld`, now deleted) to `Image`/`RectTransform`
(`RoutineCardUI`/`RoutineSlotUI`), so the old world-space cards/slots/
container won't wire up to the current scripts. Same underlying gameplay
logic either way (drag-to-reorder, 5 attempts, 2 accepted orders) — only the
presentation layer changed.

Mission ID **3** is already registered in `MissionRegistry` and in `Stage2`
(`StageData.missionIDs = [3, 4, 5]`) — nothing to change there.

Like Mission 5, this is an **advanced mission** (`MissionData.isAdvancedMission`):
there's no separate trivial-path container, and the 5 Whys quiz doesn't pick
`SolutionType` — `PlanningUI` always routes into the single
`Container_Optimal_M3`, and `FarmRoutineSystem`'s own evaluation of the
submitted order decides `wasOptimal` directly.

## A Canvas, not a separate Canvas + container

`Container_Optimal_M3` *is* a screen-space Canvas — one GameObject, not two.
Mission 5 splits `BridgeCanvas` (screen-space UI) from `Container_Optimal_M5`
(a world-space `Rigidbody2D` physics playground) because those are two
fundamentally different rendering systems that can't share a hierarchy
branch. Mission 3 has no such mixing — cards, slots, text, and the Submit
button are all UI — so there's no reason to split them. `Container_Optimal_M3`
carries the `Canvas`/`CanvasScaler`/`GraphicRaycaster` components directly,
same object, not nested under `PlayerCanvas` either. Dragging/dropping is
Unity's own `IBeginDragHandler`/`IDragHandler`/`IEndDragHandler`/
`IDropHandler` through that Canvas's `EventSystem`/`GraphicRaycaster`, which
resolves drop targets robustly for free — no hand-rolled hit-testing the way
a world-space version would need.

The world-facing part of Mission 3 — `RoutineBoardInteractable`, the board
prop in the world that starts the mission — still lives under its own
`Mission3` world group at the farm's map location, same as
`Mission1`/`Mission2`/`Mission5`'s interactables. Only the minigame container
itself is Canvas-based; starting the mission still works exactly like every
other mission (walk up, click, dialogue, Planning, 5 Whys).

## 1. The `Mission3` group (world-space)

Create a root-level `Mission3` GameObject at the farm's map location.

## 2. The routine board itself

Place a `RoutineBoardInteractable` on a routine/schedule board GameObject in
the world, under the `Mission3` group (same role as `RiverInteractable` on
the boulder / `BridgeInteractable` on the bridge — the broken *thing*, not
the farmer NPC himself, is what the player clicks). Assign
`associatedMission = M3_BrokenRoutine`.

## 3. The minigame container (`Container_Optimal_M3`)

Create a root-level GameObject called `Container_Optimal_M3` — separate from
the `Mission3` world group, not nested under `PlayerCanvas`. Add `Canvas`
(**Screen Space - Overlay**, matching `BridgeCanvas`'s render mode),
`CanvasScaler`, and it'll get a `GraphicRaycaster` automatically. Only one
container this mission — same shape as Mission 5.

**`MinigameActivator` never lives on the container it activates** (it
subscribes to `OnSolutionSelected` in its own `OnEnable`, which would never
run on an inactive container). Add a dedicated `Activator_Mission3_Optimal`
GameObject next to (not inside) `Container_Optimal_M3` — hierarchy placement
doesn't affect how `MinigameActivator` works, only the `container` field
wired below does:
- `missionID` → 3
- `container` → `Container_Optimal_M3`
- `targetState` → **`RoutineBuilder`**
- **`singleContainerForMission` → checked** — same reason as Mission 5: this
  one container can end in either outcome, and without this flag
  `MinigameActivator` only closes the container when `wasOptimal` happens to
  match its `solutionType`, leaving it stuck open on a trivial result.

**`Container_Optimal_M3` itself must start inactive**, like every other
minigame container — double check its active checkbox before playtesting.
Since it's also the Canvas here (see "A Canvas, not a separate Canvas +
container" above), there's no separate always-active wrapper to worry about
— `MinigameActivator.container.SetActive(true/false)` toggles this object
directly, same as `Container_Optimal_M1`/`M2`/`M5` for their own missions.

Put `FarmRoutineSystem` on this same root, alongside `Canvas`.

### Card prefab (one per station)

Deliberately minimal — pure background + text, no icon (there isn't an
obvious icon for an action like "Water the Crops" anyway; leave the richer
visual storytelling to a future CG rather than forcing icons that don't read
well). Create a prefab with:
- An `Image` (background) — assign to `RoutineCardUI.background`. This one
  component does double duty:
  - It's the card's actual visible rectangle — the label sits on top of it,
    same as a plain flashcard/quiz-card look. If you want rounded/framed
    corners that don't stretch when resized, use `Image.Image Type → Sliced`
    with the sprite's border set in the Sprite Editor (not `Simple`, and not
    `Tiled` — `Tiled` repeats the border/fill as multiple copies instead of
    stretching it smoothly).
  - It's also what flashes during the per-submit step-through
    (`PlayStepHighlight`) as a lightweight stand-in for the design doc's
    "small CG to show the NPC executing the tasks" — just this same image's
    color briefly shifting to `highlightColor` and back. Swap in a real
    animated CG later without touching `FarmRoutineSystem`'s ordering logic
    if you want a fuller payoff.
- A `TextMeshProUGUI` child — assign to `RoutineCardUI.label`.
- A `CanvasGroup` on the root — assign to `RoutineCardUI.canvasGroup` (used
  to disable raycasts on the card itself while it's being dragged, so it
  doesn't block its own drop target underneath it).
- The `RoutineCardUI` component itself, on the root.

Instantiate **4** card instances (one per station) as children of
`Container_Optimal_M3` (anywhere — `FarmRoutineSystem.ResetRoutine()`
reparents them onto slots on every activation/redo, so their authored
starting parent doesn't matter) and assign them, in the same order as
`stations` below, to `FarmRoutineSystem.cards`.

### Slot setup

No prefab strictly needed (neither for slots nor cards, really) —
`FarmRoutineSystem` never `Instantiate()`s a card or a slot at runtime, it
only reads whatever's dragged into its `cards[]`/`slots[]` Inspector arrays.
The card prefab above is just an Editor convenience for stamping out 4
identical-looking cards; make the slot a prefab too if you'd rather keep
sizing in sync across all 4 by hand, but it's optional.

Each slot is a plain UI `Image` GameObject (**not** just a bare
`RectTransform`) with a `RoutineSlotUI` component (`slotIndex` set per slot,
0-3 left to right). The `Image` isn't only cosmetic here — `GraphicRaycaster`
only ever hit-tests against `Graphic`-derived components (`Image`,
`TextMeshProUGUI`, ...) with **Raycast Target** checked, so a bare
`RectTransform` with no `Graphic` would never register a drop at all;
`OnDrop` simply wouldn't fire. Give it a faint/low-alpha color so an empty
vs. occupied slot both read clearly, but functionally it has to exist and
stay raycast-targetable regardless. `RoutineSlotUI` itself doesn't reference
this `Image` through any serialized field — it only needs to be present on
the same GameObject for the raycaster to find something to hit.

No `CanvasGroup` needed on a slot (that's a drag-only concern, see the card
above) and no child object either — one flat `Image` + `RoutineSlotUI` is
the whole prefab. Placed cards reparent *into* their slot
(`FarmRoutineSystem.PlaceCardInSlot`), and Unity UI always draws a child
after its parent, so a card automatically renders on top of its slot's
background with no sorting-order management needed.
A `Horizontal Layout Group` on the row's parent is a reasonable way to keep
the 4 slots evenly spaced without hand-positioning each one.

### Status/attempts/hint text and Submit button

Add a `RoutineBuilderUI` component to `Container_Optimal_M3`'s root (or a
child), wire `system` to `FarmRoutineSystem`, the three
`TextMeshProUGUI` fields (`statusText`/`attemptsText`/`hintText`), and a
**Submit** `Button` wired to `RoutineBuilderUI.OnSubmitPressed` in the
Inspector (same "buttons call straight into the owning system" pattern as
`BridgeBuilderUI`/`DayCompleteUI`/`InfoBoardUI`). `hintText` should default
to inactive — `RoutineBuilderUI` only activates it when
`StageManager.IsMissionUnderReview(3)` is true (the design doc's "hint on the
second attempt").

## 4. `FarmRoutineSystem` fields

- `missionID` → 3.
- `missionData` → `M3_BrokenRoutine` (only source of `minigameHint` — see the
  Data Layer note below).
- `stations` — 4 entries, each just a `stationName`. "Morning Chores →
  Harvest Chain" (any order here — the *array index* is each station's
  permanent identity, referenced by `acceptedOrders` below, not the order you
  list them in the Inspector):
  0. Feed the Animals
  1. Water the Crops
  2. Harvest the Crops
  3. Sell at Market
- `cards` — the 4 card instances from above, **in the same order as
  `stations`** (`cards[i]` is always initialized as `stations[i]` — see
  `FarmRoutineSystem.Awake`).
- `slots` — the 4 `RoutineSlotUI`s, left to right.
- `acceptedOrders` — the design doc's "2 desirable outcomes". Feed/Water are
  same-morning chores that don't depend on each other (either can go first);
  Harvest only makes sense once the crops have been watered; Sell only makes
  sense once there's a harvest to sell. With the station indices above:
  - `[0, 1, 2, 3]` — Feed → Water → Harvest → Sell
  - `[1, 0, 2, 3]` — Water → Feed → Harvest → Sell
- `baseAttempts` (default 5) — matches the design doc's "5 tries for the
  player" directly.
- `bonusAttemptsPerCorrectWhy` (default 1) — same idea as
  `BridgeBuilderSystem.bonusAttemptsPerCorrectWhy`: a strong 5 Whys diagnosis
  earns extra attempts on top of the base 5, even though this mission's quiz
  doesn't pick trivial vs. optimal either. Set to 0 if you'd rather the base 5
  be a hard cap with no bonus.
- `stepDelay`/`resultHoldDuration` — pacing for the per-submit "small CG"
  stand-in (each station highlights in sequence, then the pass/fail message
  holds before the panel accepts another try).
- `stepSfx`/`successSfx`/`failSfx` — optional, played during
  `SimulateAndResolve`.

## 5. Data Layer note

`MissionData.minigameHint` is a new field (see `MissionData.cs`), only used
by advanced missions whose action-phase minigame isn't structured as discrete
Why stages the way the 5 Whys quiz is (so there's no per-question `hint` slot
to reuse). It's already authored on `M3_BrokenRoutine`. `M5_BrokenBridge`
leaves it blank — Mission 5 hints via bonus test attempts instead, not text.

## 6. Mission Directory HUD

Add a `MissionDirectoryUI.DirectoryEntry` for `M3_BrokenRoutine` with its own
`TextMeshProUGUI` line (this one's on the screen-space HUD under
`PlayerCanvas`, unrelated to `Container_Optimal_M3`'s own Canvas above), same
as the existing missions. `FarmRoutineSystem` raises `OnObjectiveProgress` at stage index 0
only (both on activation/reset and after every failed submit), matching
`M3_BrokenRoutine.optimalObjectives[0]`'s `{0}/{1}` placeholders (attempts
used / max attempts). `trivialObjectives` is intentionally empty — same
reason as Mission 5: the single container's
`MinigameActivator.solutionType` is always `Optimal`, so a trivial-path line
never gets raised.

## 7. Tuning knobs (playtest and adjust)

All on `FarmRoutineSystem`:
- `baseAttempts` / `bonusAttemptsPerCorrectWhy` — see above.
- `stepDelay` (default 0.5s) / `resultHoldDuration` (default 1s) — the
  per-submit pacing.

Everything downstream of `OnMissionCompleted` (reflection text, coin reward,
trust, Stage Gate redo, Mission Directory resolved state) is already wired
for free and needs no advanced-mission-specific handling, same as Mission 5 —
those systems only ever cared about the final `wasOptimal`. The 5 Whys quiz
still runs for `M3_BrokenRoutine` exactly as authored; it just feeds
`bonusAttemptsPerCorrectWhy` instead of picking the path (see
`PlanningUI.SelectAdvancedMission`).
