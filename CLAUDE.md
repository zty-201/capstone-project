# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6 2D educational game built around Kaizen/PDCA methodology. Players explore a village, talk to NPCs with problems, choose between a trivial or optimal solution, complete a minigame, and receive reflective feedback. All game scripts live under `Assets/Scripts/`.

See `Docs/GameDesignDocument.md` for the full design rationale and `Docs/TODO.md` for planned/not-yet-implemented features.

## Development

Open the project in the Unity Editor (Unity 6). There are no CLI build or test commands — all iteration happens inside the Editor. Scripts are compiled automatically on save.

## Coding Conventions

- Don't overengineer: Simple beats complex
- No fallbacks: One correct path, no alternatives
- One way: One way to do things, not many
- Match existing structure: When a new feature could reasonably be built more than one way, prefer whichever way is consistent with how similar things already work in this codebase — even over an option that's more "correct" in the abstract. Consistency for future maintainers outranks textbook-ideal architecture.
- Clarity over compatibility: Clear code beats backward compatibility
- Throw errors: Fail fast when preconditions aren't met
- No backups: Trust the primary mechanism
- Separation of concerns: Each function should have a single responsibility
- Surgical changes only: Make minimal, focused fixes
- Evidence-based debugging: Add minimal, targeted logging
- Fix root causes: Address the underlying issue, not just symptoms
- Simple > Complex: Let TypeScript catch errors instead of excessive runtime checks
- Collaborative process: Work with user to identify most efficient solution
- When you are uncertain about facts, current information, or technical details, you should use web search to verify and provide accurate information rather than speculating or admitting uncertainty without investigation. When a problem seems to involve a specific API or library, don't assume you know it. Always check the web for the documentation of the relevant features.


## Architecture

### State Machine
`GameManager` (singleton MonoBehaviour) owns a `GameStateManager`, which holds a `Dictionary<GameStateType, IState>`. Every distinct mode of play is an `IState` with `Enter()`, `Tick()` (called from `Update`), and `Exit()`. States never reference each other directly — they call `GameManager.Instance.StateManager.ChangeState(...)` or fire events.

**States and their responsibilities:**
| State | What it does |
|---|---|
| `Exploration` | Polls mouse clicks, fires `RaiseMapClicked` |
| `Dialogue` | Delegates left-click to `DialogueManager.OnAdvanceDialogue()` |
| `Planning` | Delegates left-click to `PlanningUI.OnAdvance()`; ESC returns to Exploration |
| `Puzzle` | Polls mouse clicks, fires `RaisePuzzleClicked` for the pipe puzzle |
| `Reflection` | Delegates left-click to `ReflectionPopupUI.OnDismiss()` |
| `MissionBoard` | ESC returns to Exploration |
| `DayComplete` | Empty stub — the day-complete panel is dismissed via a UI Button wired directly to `DayCompleteUI.OnDismiss()` in the Inspector, not through `Tick()` |
| `InfoBoard` | ESC returns to Exploration (same shape as `MissionBoard`) |

### Event Bus
`EventBus` is a static class of C# events. Systems subscribe in `OnEnable`/`OnDisable` and raise via the `Raise*` helpers. This is the only coupling layer between systems — no direct references across domains.

Key events: `OnMapClicked → OnPathRequested → OnPathGenerated`, `OnSolutionSelected`, `OnMissionCompleted`, `OnMissionsNeedReview`, `OnDayCompleted`, `OnInventoryChanged`, `OnTrustChanged`, `OnPDCAPhaseChanged`, `OnObjectiveProgress`.

### Mission Flow (complete happy path)
1. Player clicks NPC (`IInteractable.Interact()`) → `DialogueState`
2. `DialogueManager` exhausts all lines → opens `PlanningUI` → `PlanningState`
3. `PlanningUI` type-animates trivial solution name, then optimal solution name, then runs the 5 Whys quiz (see below), which determines the outcome — the player no longer manually picks trivial vs. optimal
4. On the 5th why, `PlanningUI` itself calls `RaiseSolutionSelected(missionID, type)` (5/5 correct → Optimal, anything less → Trivial) → `MinigameActivator` activates the right container and changes to its inspector-assigned `targetState`
5. Puzzle solved / well patched → `RaiseMissionCompleted(missionID, wasOptimal)`
6. `ReflectionPopupUI` listens, shows feedback text from `MissionData`, changes state to `Reflection`
7. Player clicks to dismiss → `Exploration`; `MissionBoardUI` listens to grey out the entry

A trivial resolution isn't necessarily final — see Stage Gate System below for how submitting a stage at Town Hall with any trivial mission still outstanding reopens it for a redo.

### PDCA Phase Indicator
A HUD element (`PDCAIndicatorUI`) makes the Plan-Do-Check-Act framing visible while playing,
driven entirely by `EventBus.OnPDCAPhaseChanged(PDCAPhase)` rather than `GameStateType` — most of
the "Do" minigames (well patch, waste pickup, part collection) run inside plain `Exploration`
state with no dedicated `GameStateType` of their own, so the indicator can't be state-driven the
way `GameStateManager` is. `PDCAPhase` is `{ None, Plan, Do, Check }` — "Act" is deliberately not
a distinct visible phase; the indicator just hides (`None`) on return to Exploration, standing in
for "go apply what you learned" without a dedicated screen to anchor a 4th label to. Four
single-line raise points, chosen as the exact mission-scoped moment each phase begins:
`PlanningUI.Show(mission)` → `Plan`; `MinigameActivator.HandleSolutionSelected` (right after
activating `container` — the universal entry point for all four trivial/optimal paths across both
missions) → `Do`; `ReflectionPopupUI.HandleMissionCompleted` → `Check`;
`ReflectionPopupUI.OnDismiss()` → `None`.

### Mission Directory HUD
A top-left HUD element (`MissionDirectoryUI`) shows a compressed one-line-per-active-mission
objective tracker, tracking progress *within* a mission's Do phase (e.g. "Collect the parts to
build the machine (0/3)") rather than just whether the mission is done — finer-grained than
`PDCAPhase`/`OnMissionCompleted` can express, since e.g. "0/3 parts" vs. "assemble" vs. "place"
are all still `PDCAPhase.Do` and all still the same `missionID`. Line content is authored data on
`MissionData` (`introObjective`, `trivialObjectives[]`, `optimalObjectives[]`), matching the
existing convention of keeping mission text out of code (villagerComplaint, fiveWhys, reflection
texts); progress is signaled purely through `EventBus.OnObjectiveProgress(missionID, path,
stageIndex, count, total)`, matching EventBus as the sole coupling layer — `MissionDirectoryUI`
never holds a reference into a mission script. A line containing `{0}`/`{1}` is run through
`string.Format` with `count`/`total`; a plain line ignores those args. The event carries its own
`SolutionType` rather than the UI inferring the active path from a separate
`OnSolutionSelected` subscription — that would race against `MinigameActivator` raising this same
event on the same frame, since subscriber order between two different EventBus events isn't
guaranteed. Every raiser is single-path by construction (e.g. `PartCollectionSystem` only ever
runs as part of the Optimal path), so each just passes its own path literally.

Raise points: `MinigameActivator.HandleSolutionSelected` raises stage 0 with no count right
before `container.SetActive(true)` — the same universal Do-phase entry point `PDCAPhase.Do` uses
(see above) — and a counted first stage (`PartCollectionSystem`/`WastePickupSystem`) overwrites it
with the real count from its own `OnEnable`, which fires synchronously inside that `SetActive`
call and so always runs after. `PartCollectionSystem.OnPartCollected` re-raises stage 0 on every
pickup and raises stage 1 once `assemblyPoint` activates; `AssemblyPoint.Interact` raises stage 2;
`WastePickupSystem.OnWasteRemoved` re-raises stage 0 on every pickup. Mission 1's two paths (well
patch, pipe puzzle) have no sub-stage granularity worth tracking, so their `trivialObjectives`/
`optimalObjectives` arrays are single-entry — `MinigameActivator`'s stage-0 raise is the only one
they need.

A resolved-optimal mission's line disappears (nothing left to track); a resolved-trivial mission's
line reads "Needs Review" (matching `MissionEntryUI`'s exact wording for the same outstanding
state). On `OnMissionsNeedReview`, the line resets to `introObjective` rather than resuming
mid-path — a redo starts back at square one (re-interact to re-open dialogue), so there's no
sub-stage state worth preserving across the reset.

### NPC Trust Meter
`TrustSystem` (singleton) tracks a `0..maxTrust` (default 5, starting at 2) trust value per
`missionID`, listening to `OnMissionCompleted`: `+trustGainOnOptimal` if optimal,
`-trustLossOnTrivial` if trivial, clamped, raising `EventBus.OnTrustChanged(missionID, newTrust)`.
It's intentionally **visual-only** — trust reflects mission outcome history but doesn't gate
anything; reattempting a trivial mission is still handled entirely by the Stage Gate System above.
Trust also persists across stages/days (not reset by `SubmitStage()`), since it's a standing
relationship signal rather than per-stage bookkeeping. `NPCTrustUI` is a companion component
(same "attach alongside, don't couple to" pattern as `InteractionIndicator`) on `NPCController`
(mission 1) and `RiverInteractable` (mission 2), rendering trust as a row of pip `SpriteRenderer`s
(toggled via `.enabled`, not UI `Image`s — see World-Attached NPC UI below). It reads
`TrustSystem.Instance.GetTrust(missionID)` in `Start()` rather than `OnEnable()` — Unity
guarantees every object's `Awake` runs before any object's `Start`, so this is safe without the
`OnEnable`-ordering workaround `NPCPatrol` needs for `GameManager.Instance`.

### World-Attached NPC UI
World-attached indicators (the prompt icon, trust pips) default to `SpriteRenderer` + sorting
layer, the same rendering system every other world object in the scene already uses — not a
World Space `Canvas`. Escalate to a World Space `Canvas` only when an element needs a genuine
UI-only capability `SpriteRenderer` can't do (`Image.fillAmount`, layout groups, interactive
widgets); floating dynamic text uses mesh-based `TextMeshPro`, not `TextMeshProUGUI`, so text
alone isn't a reason to escalate either. This keeps "how do I show something above an object in
the world" answered one way, consistent with the One Way convention above, rather than splitting
world-attached visuals across two parallel rendering systems.

### 5 Whys Quiz (PlanningUI)
After typing both solution names, `PlanningUI` runs 5 sequential "Why" stages sourced from `MissionData.fiveWhys` (a `WhyStage[5]`, each with `question`, `correctAnswer`, `distractors[]`, `hint`). Picking any option always advances to the next stage — there is no blocking retry on a wrong pick — but `PlanningUI` tallies `correctCount` across the 5 stages. After the last stage, `outcomeIsOptimal = correctCount >= 5`, and that's what gets passed to `RaiseSolutionSelected`; hitting all 5 is intentionally hard.

The quiz behaves differently on a redo (see Stage Gate System below): `hintText` only shows `WhyStage.hint` when `StageManager.IsMissionUnderReview(missionID)` is true, so a first attempt gets no hint but a review redo does; and each stage's distractor pool excludes whatever the player already picked wrong on a prior attempt at that stage (`StageManager.RecordWrongAnswer`/`GetExcludedDistractors`), with a floor guard so a question never collapses down to just the correct answer alone.

### Mission Board
`MissionBoardUI` holds one `MissionEntryUI` per mission (assigned in Inspector). On `OnMissionCompleted`, the matching entry greys out (`alpha = 0.4`) and its status label reads "Resolved" (optimal) or "Needs Review" (trivial). On `OnSolutionSelected`, `NPCController.HandleSolutionSelected` sets an internal `missionCompleted` flag (so `Interact()` becomes a no-op) and hides its `InteractionIndicator`, but the NPC's GameObject itself stays active — it remains visible (and keeps patrolling, if it has an `NPCPatrol`) rather than disappearing.

A "Needs Review" (trivial) mission *can* now be reopened, but only through the Stage Gate System (see below) rejecting a stage submission at Town Hall — there's no way to manually revisit a trivial mission before then. Once `OnMissionsNeedReview` fires for it, `MissionEntryUI.ResetVisual()` un-greys the entry and the mission's own interactable/minigame resets itself so it can be replayed.

### Data Layer (ScriptableObjects)
- **`MissionData`** — all text content for one mission: complaint, root cause, 5 Whys quiz data (`fiveWhys: WhyStage[5]`, each with `question`/`correctAnswer`/`distractors[]`/`hint`), solution names, reflection texts. Create via `Kaizen Systems/Mission Data`. `M1_ParchedCrops` and `M2_CleaningRiver` have their 5 Whys chains populated, each one ending at the mission's `actualRootCause`.
- **`MissionRegistry`** — array of `MissionData`, looked up by `missionID`. Create via `Kaizen Systems/Mission Registry`. Assign in Inspector on `ReflectionPopupUI`.
- **`StageData`** — one stage's `stageNumber`, `stageName`, and `missionIDs[]` (the missions that must all be resolved optimally before the stage can be submitted). Create via `Kaizen Systems/Stage Data`.
- **`StageRegistry`** — array of `StageData`, looked up by index (`GetByIndex`). Create via `Kaizen Systems/Stage Registry`. Assign in Inspector on `StageManager`.
- **`ItemData`** — one inventory item's `itemID`, `itemName`, `icon`, and stacking rules (`stackable`, `maxStack`). Create via `Kaizen Systems/Item Data`. Five assets exist: **Gold Coin** (stackable) and **Trash** (not stackable, so litter piles up one slot per piece), assigned in Inspector on `CoinRewardSystem`, `TrashPiece`, `TrashCollectionSite`, and `StageManager`; **Brick** (not stackable, only ever one needed) for Mission 1's trivial fetch quest, assigned on `BrickPickup` and `WellPatchSite`; **Machine Part** (stackable) and **Winch** (not stackable) for Mission 2's optimal path, assigned on `MachinePart`, `AssemblyPoint`, and `PlacementPoint` — `AssemblyPoint.Interact()` trades 3 Machine Parts for 1 Winch, and `PlacementPoint.Interact()` consumes the Winch on final placement.

### Stage Gate System
`StageManager` (singleton) groups missions into stages via `StageData`, tracks each mission's most recent outcome (`missionOutcomes: Dictionary<int, bool>`), and gates day advancement on every mission in the current stage having been resolved *optimally* — resolving a mission trivially no longer quietly counts toward finishing the day.

**Town Hall interact routes through the gate**, not straight to `RaiseDayCompleted` anymore. `TownHallInteractable.Interact()` checks, in order: `StageManager.AllStagesComplete` (shows a closing-out dialogue and stops), `StageManager.AllMissionsCompleteForCurrentStage()` (shows an "outstanding problems" dialogue if any mission in the stage hasn't been completed at all yet), `TrashSpawner.Instance.HasLiveTrash` (shows a "clear the streets" dialogue if any trash piece is currently on the ground), `StageManager.AllMissionsOptimalForCurrentStage() && !StageManager.HasEnoughCoins()` (shows a "bring two gold coins" dialogue if every mission is optimal but the player isn't carrying enough — see Gold Coin Economy below) — and only calls `StageManager.SubmitStage()` once all four pass.

**`SubmitStage()`** partitions the stage's missions into those completed optimally and those still flagged trivial:
- **All optimal** → consumes `coinsRequiredToSubmit` Gold Coins from `InventorySystem` (guaranteed to succeed — `TownHallInteractable` already confirmed there are enough before calling in), advances `currentDay`, raises `OnDayCompleted`, clears `missionOutcomes`/`excludedDistractors`, and advances `currentStageIndex` (or sets `AllStagesComplete` once the registry is exhausted).
- **Some still trivial** → raises `OnMissionsNeedReview(int[] missionIDs)`. Trivial completions never earned a Gold Coin in the first place (see below), so there's nothing to retract on a failed redo — the coin count simply reflects however many missions have been solved optimally so far.

**`OnMissionsNeedReview` reopens the flagged missions in place.** Every mission-specific system listens for it and resets itself to pre-completion state: `NPCController` (Mission 1's NPC) clears `missionCompleted` and calls `InteractionIndicator.ResetVisibility()`; `RiverInteractable` (Mission 2's trigger) re-`SetActive(true)`s itself; `PipePuzzleSystem` resets every `PipeVisual` to its cached original rotation/bitmask (`ResetPuzzle()`); `PartCollectionSystem`/`WastePickupSystem` reset their collected counts and re-show their pieces (`MachinePart.ResetPart()`, `WastePiece.ResetPiece()`, `AssemblyPoint.ResetPoint()`, `PlacementPoint.ResetPoint()`); `MissionBoardUI`/`MissionEntryUI` un-grey the entry (`ResetVisual()`). Components living inside a container `MinigameActivator` disables after mission completion (`PartCollectionSystem`, `WastePickupSystem`) or that disable themselves on completion (`RiverInteractable`, `PipePuzzleSystem`) subscribe to `OnMissionsNeedReview` in `Awake`/`OnDestroy` rather than `OnEnable`/`OnDisable`, since an `OnEnable`/`OnDisable` subscription would already be torn down by the time a review request — which can only happen after the mission is complete — needs to reach it.

**A redo runs through the same 5 Whys quiz** with hint/distractor differences from a first attempt — see 5 Whys Quiz above.

**`TrashSpawner`** is a singleton (`Instance`) exposing `HasLiveTrash`; it also listens for `OnDayCompleted` and destroys every live *ground* trash piece (litter never picked up). It does not touch trash already sitting in the player's inventory — that only clears at the Trash Collection Site, see Gold Coin Economy & Inventory below. A mission being flagged for review does *not* clear trash — only a full stage pass does.

### Pathfinding & Grid
- **`GridSystem`** (pure C#) — 2D array of `GridNode`; converts between world positions and grid coordinates.
- **`PathfindingSystem`** (MonoBehaviour) — builds a `GridSystem` in `Start`, reads a collision `Tilemap` to mark unwalkable cells, runs A* using a `NodeMinHeap` min-heap. Two entry points into the same A* core: the `EventBus.OnPathRequested` handler (`CalculatePath`) fires `RaisePathGenerated` with the result and is what `PlayerController` uses; `RequestPathSync(start, end)` returns the path directly instead of broadcasting it, for callers that must not go through the shared event pair (see NPC Patrol below — every subscriber receives every `OnPathGenerated`, so a second broadcaster would make the player walk an NPC's path or vice versa). `GetRandomWalkableCoordinates(Vector2Int from)` does a BFS flood-fill from `from` over walkable neighbors and returns a random cell from the reachable set, so callers never get handed a walkable "island" cell that's cut off by unwalkable tiles (which would otherwise burn a full failed A* search and log a warning). Exposes `SetWalkable(Vector3 worldPos, bool walkable)` to toggle grid cells at runtime (e.g. doors, mission triggers).
- **`InputManager`** — converts `OnMapClicked` world positions to grid coords; if an `IInteractable` is adjacent it calls `Interact()`, otherwise fires `RaisePathRequested`.
- **`PlayerController`** — listens to `OnPathGenerated`, walks the path via a coroutine, flips the `SpriteRenderer` on horizontal movement. Before each step checks the next cell via `Physics2D.OverlapPoint` against an `npcLayerMask`; if blocked, it `yield return null`s once before re-requesting the path from the current position, so the player reroutes around moving entities. That single-frame wait is load-bearing, not cosmetic: re-requesting synchronously can recurse into `StartCoroutine(FollowPath(...))` again within the same call stack, and if the newly computed path is blocked at its own first step too (e.g. a patrol NPC parked on the only route), it recurses without ever yielding and overflows the native stack.

### Mission 2: The Stagnant Pond
The map's river now runs from a cliff-top source down a waterfall into a village pond. The
interactable that starts the mission is `RiverInteractable`, positioned on the boulder wedged at
the lip of the falls (not an NPC) — a natural rockslide, not litter or a human cause. Both
solutions run inside `ExplorationState` — no new game states needed. Two additional
`ContextInteractable` points sit nearby (the thinned-out riverbed below the falls, villagers
complaining that the pond has gone stagnant and unsafe to drink/wash in) purely to give the
player narrative context before they attempt the 5 Whys quiz — they show dialogue and return
straight to `Exploration`; they don't reference a `MissionData` or touch mission state at all.

**Trivial — Clear the Loose Rubble:** `MinigameActivator` activates `TrivialContainer`, which
holds `WastePickupSystem` and a set of `WastePiece` IInteractables overlapping loose rubble shaken
free by the rockslide (not litter). Each `WastePiece.Interact()` hides its paired `wasteVisual`
and calls `WastePickupSystem.OnWasteRemoved()`. When remaining count hits zero, fires
`RaiseMissionCompleted(2, false)` — enough rubble clears for a trickle, but the wedged boulder
itself stays put, so the pond keeps stagnating.

**Optimal — Rig a Cliffside Winch:** `MinigameActivator` activates `OptimalContainer`, which holds
`PartCollectionSystem` and 3 `MachinePart` IInteractables placed at fixed positions in the editor.
Each `MachinePart.Interact()` collects itself and calls `PartCollectionSystem.OnPartCollected()`.
At 3/3, `PartCollectionSystem` activates `AssemblyPoint` near the cliff lip. `AssemblyPoint.Interact()`
shows the assembled winch visual and activates `PlacementPoint` at the falls. `PlacementPoint.Interact()`
shows the anchored winch visual and fires `RaiseMissionCompleted(2, true)` — the winch levers the
boulder free and stays bolted in place to catch whatever comes down next.

**River reveal:** `RiverManager` listens to `OnMissionCompleted` for missionID 2. On either
solution: disables `blockageVisual` (the wedged boulder), enables `animatedRiverTilemap` — the
falls resume flowing into the pond either way, since the visual payoff is identical regardless of
path; only the reflection text (and whether a future slide gets caught automatically) differs.

### Mission 1: Well & Pipe Puzzle
Like Mission 2's river, the well itself (not an NPC) is the interactable that starts the
mission — `NPCController` (`Mission1NPCInteractble.cs`, historically written for a wandering
villager) is attached directly to the well's `GameObject` rather than a separate Farmer NPC.
Since two things now need to be clickable at the same world position (the dialogue trigger,
then whatever the chosen path activates there), `HandleSolutionSelected` disables the well's
own `Collider2D` once a solution is picked — `Physics2D.OverlapPoint` doesn't guarantee which
of two perfectly-overlapping colliders it returns, so leaving both live would make clicks land
on the wrong one unpredictably. `HandleMissionsNeedReview` re-enables it for a stage-gate redo.

**Trivial — Fetch a Brick, Patch the Well:** a two-stage fetch quest through the real
inventory (see Gold Coin Economy & Inventory below), matching Mission 2 optimal's "collect
physical items, bring them to one spot" shape rather than resolving in a single click.
`BrickPickup` (`IInteractable`, placed elsewhere on the map) is the trivial container's
stage-0 piece: same gate-on-inventory-success shape as `TrashPiece` — `Interact()` only
removes itself and advances the objective if `InventorySystem.TryAddItem` actually succeeds.
`WellPatchSite` (`IInteractable`, at the well, inside `Container_Trivial_M1`) is stage 1:
`Interact()` is a no-op unless the player is carrying a Brick, otherwise it consumes one
(`TryRemoveItem`), shows the patched-well visual, and raises `RaiseMissionCompleted(id,
false)`. Runs entirely inside `ExplorationState`, no dedicated state needed — same pattern as
Mission 2. (A drag-and-drop version of the patch step — dragging a brick sprite onto a hole
with snap-to-place — was prototyped and reverted: dragging was the only interaction of its
kind anywhere in the game and read as tonally distorted next to every other walk-up-and-click
resolution. There also used to be a separate `PatchWellState` gating the *old* single-click
patch via a bespoke `RaiseWellClicked` event; it was removed because that state never allowed
player movement, so if the mission-triggering NPC had wandered away from the well before
dialogue started, the player could get stranded unable to reach it — the fetch-quest redesign
inherits that same "no dedicated state" reasoning.)

**Optimal — Pipe Puzzle:** `PipeDirection` is a `[Flags]` bitmask enum (Up=1, Right=2, Down=4,
Left=8). `PipeNode` holds the current connection bitmask and rotates clockwise via a left
bit-shift with wrap-around (`(bits << 1 | bits >> 3) & 15`). `PipeVisual` (MonoBehaviour) reads
its `PipeShape` and inspector transform rotation to compute starting bits from a hardcoded
canonical-bits-per-shape switch (e.g. `Corner` = `Down|Right` at 0° rotation) — this must match
what the shape's sprite actually draws at 0°, since the rotation math only ever rotates *that*
canonical, never inspects the art; a mismatch (found and fixed for `TJunction`, whose sprite is
`Left|Right|Down` at 0° rather than the code's original `Up|Right|Down`) desyncs the visual
rotation from the logical connections by a fixed step at every angle rather than just being
cosmetically wrong. `Cross` is rotation-invariant (`Up|Right|Down|Left` always) — any authored
rotation works. Clicks delegate to `PipePuzzleSystem.RotatePipeAt` via the dedicated `Puzzle`
state/`RaisePuzzleClicked` event (no adjacency requirement — a precise click on a pipe tile
rotates it regardless of player position). The puzzle system runs a DFS flood-fill from
`startPos` to `endPos` to check for a valid water path after every rotation, and raises
`RaiseMissionCompleted(id, true)` once solved. The puzzle board is a full 5×5 grid (all four
`PipeShape`s in play — `Straight`, `Corner`, `TJunction`, `Cross`); not every cell needs a pipe
(`PipePuzzleSystem` only populates grid cells where a `PipeVisual` actually exists — an
unfilled cell is just `null` and the flood-fill skips it).

### Singletons
`GameManager`, `DialogueManager`, `PlanningUI`, `MissionBoardUI`, `ReflectionPopupUI`, `DayCompleteUI`, `InventorySystem`, `TrustSystem`, `InfoBoardUI`, `StageManager`, `TrashSpawner` all follow the same pattern: static `Instance`, destroyed if a duplicate exists in `Awake`.

### IInteractable
`NPCController`, `MissionBoardInteractable`, `RiverInteractable`, `WastePiece`, `MachinePart`, `AssemblyPoint`, `PlacementPoint`, `TrashPiece`, `TrashCollectionSite`, `TownHallInteractable`, `ContextInteractable`, `BrickPickup`, `WellPatchSite`, and `InfoBoardInteractable` all implement `IInteractable`. `InputManager` detects them via `Physics2D.OverlapPoint` and calls `Interact()` when the player is within 1 grid cell (or routes the player adjacent first). `ContextInteractable` is the odd one out: it's narrative-only (dialogue with no associated `MissionData`), so `DialogueManager` returns straight to `Exploration` afterward instead of opening `PlanningUI` — it never starts or resolves a mission.

### Info Board
A walk-up-and-interact help/tutorial panel, architecturally a clone of the Mission Board: `InfoBoardInteractable` (`IInteractable`) shows `InfoBoardUI` and changes state to `InfoBoard`; `InfoBoardState` is ESC-only, same shape as `MissionBoardState`. `InfoBoardUI` isn't dialogue-typed — it's a static paged reference (`InfoPage[] pages`, each a `title`/`body`), navigated with Next/Previous buttons wired directly to `ShowNextPage()`/`ShowPreviousPage()` in the Inspector, covering movement, the 5 Whys mechanic, the PDCA cycle, gold coins & trust, trash & inventory, town hall, and a catalog of interactable types. The default page content is a C# field initializer on `InfoBoardUI.pages`, not scene-authored data.

### Minimap
`MinimapCamera` (on a dedicated second `Camera` in the scene) follows the `Player`-tagged object every `LateUpdate`, using the same tag-lookup pattern as `InteractionIndicator`. That camera renders to a RenderTexture displayed by a `RawImage` anchored top-right on the main Canvas — it's a live zoomed-out view of the same scene, not a separate icon-based map.

`InteractionIndicator` is an optional companion component placed on an interactable's GameObject: it shows a bobbing prompt icon whenever the player is within `showRange` during `Exploration`. Call its `Hide()` method once the owning interactable has been permanently consumed (e.g. a collected `MachinePart`, or an `NPCController` whose mission was just resolved — see Mission Board below).

### NPC Patrol
`NPCPatrol` is a standalone component (added alongside `NPCController` on the same NPC GameObject) that wanders an NPC between random walkable tiles. Its coroutine `yield return null`s once before its first loop iteration, because `OnEnable` isn't guaranteed to run after every other object's `Awake` — only that object's own `Awake` is guaranteed before its own `OnEnable`, so touching `GameManager.Instance` synchronously in `OnEnable` can NRE if `GameManager`'s `Awake` hasn't run yet. Each loop iteration: gate on `GameManager.Instance.StateManager.CurrentStateType == GameStateType.Exploration` (idles otherwise, so NPCs freeze mid-step the instant dialogue/a minigame opens rather than sliding around during it), pick a destination via `PathfindingSystem.GetRandomWalkableCoordinates`, path to it via `RequestPathSync`, then step along it with the same move/animate/flip pattern as `PlayerController.FollowPath`. Disabling the NPC's GameObject (e.g. `SetActive(false)`) stops this coroutine for free.

NPCs that should block the player's path (and be avoided by the reroute-on-block logic in `PlayerController`) need a `Collider2D` on a dedicated `NPC` Unity layer, with `PlayerController.npcLayerMask` including that layer. The collider should be a trigger — the avoidance is handled by path-rerouting, not physics collision response.

### Gold Coin Economy & Inventory
There is no abstract progress meter — `TownSatisfactionSystem`/`SatisfactionBarUI` were removed
outright and replaced with a real inventory the player carries.

**`InventorySystem`** (singleton) owns a fixed array of 8 `InventorySlot` (plain `ItemData item` +
`int count`, not a `MonoBehaviour`). `TryAddItem(ItemData, amount)` stacks into an existing slot
if `item.stackable` and there's room, otherwise claims the first empty slot; returns `false` if
nothing fits. `CountItem`, `TryRemoveItem`, and `RemoveAllOfItem` round out the API. Every
mutation raises `EventBus.OnInventoryChanged` (no payload — subscribers just re-read `Slots`).
`InventoryUI` (HUD element, occupies the screen position the satisfaction bar used to) is a fixed
array of slot `Image`/count-text pairs that refresh on that event — the same fixed-array pattern
as `MissionBoardUI.missionEntries`.

**`CoinRewardSystem`** listens to `OnMissionCompleted` and calls
`InventorySystem.TryAddItem(goldCoinItem, 1)` only when `wasOptimal` — a trivial completion earns
no coin at all, so there's nothing to claw back if that mission later gets reattempted. The Gold
Coin `ItemData` is stackable, so every coin the player is carrying lives in a single inventory
slot.

**Trash** is now something the player physically carries rather than a satisfaction penalty.
`TrashSpawner` periodically instantiates a `trashPrefab` at a random unoccupied point from its
`spawnPoints` array — spawning is purely presence-based now, no numeric penalty on spawn. The
spawn timer lives in `Update()`, gated by
`GameManager.Instance.StateManager.CurrentStateType != GameStateType.Exploration` (early return),
so spawning pauses during any non-Exploration state and resumes only in `Exploration`.
`TrashPiece.Interact()` tries `InventorySystem.TryAddItem(trashItem, 1)` (the Trash `ItemData` is
**not** stackable, so every piece claims its own slot — letting litter pile up meaningfully
crowds out Gold Coins); on success it removes itself from the spawner's occupied set and destroys
its GameObject exactly as before, on failure (inventory full) it's left on the ground untouched.
**`TrashCollectionSite`** is a plain `IInteractable` (same shape as `WellPatchSite`/
`RiverInteractable`) placed in the village — one interact calls
`InventorySystem.RemoveAllOfItem(trashItem)`, clearing every trash slot at once.

**Spending coins**: see Stage Gate System above — `StageManager` requires
`coinsRequiredToSubmit` (2) Gold Coins on hand, on top of every mission being optimal, before
`TownHallInteractable` will let `SubmitStage()` run.

### Day Progression & Town Hall Upgrade
Day-end is player-controlled, and now gated by the Stage Gate System above: `TownHallInteractable` (on the `TownHall` GameObject, alongside `TownHallUpgrade`) routes `Interact()` through `StageManager.SubmitStage()` instead of firing `RaiseDayCompleted` unconditionally. Only a full stage pass (every mission in the current stage resolved optimally, no live trash) actually advances the day; walking up to Town Hall before that just shows a dialogue explaining what's still outstanding.

`DayCompleteUI` handles two distinct outcomes:
- `OnDayCompleted` (stage passed): shows a flat congratulatory subtitle — passing already implies every mission was optimal and both Gold Coins were paid in, so there's no separate score left to tier.
- `OnMissionsNeedReview` (stage rejected): shows a distinct "Needs Review" panel instead, reporting the count of missions still flagged.

Both handlers change state to `GameStateType.DayComplete`.

`TownHallUpgrade` (on the town hall entity) listens to `OnDayCompleted(int day)` and activates the matching index in its `stages` array, deactivating all others. Index 0 = default, index 1 = Day 1 upgrade, index 2 = Day 2 upgrade. The town hall is built as a multi-child SpriteRenderer GameObject (not tilemaps) so each stage can have a Base sprite (EntityTilemap sorting layer) and a Roof sprite (ForeGroundTilemap sorting layer) to preserve player depth layering.

`EventBus.OnDayCompleted` (`Action<int>`) is the hook for any other system that needs to respond to day advancement.
