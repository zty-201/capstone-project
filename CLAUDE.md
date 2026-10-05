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
- Scope of these conventions: "One way" / "Match existing structure" govern code architecture and implementation only. They do not apply to game/mission design. When brainstorming new missions, mechanics, or design directions, give unbiased design recommendations from a game-design point of view, even if the result looks nothing like an existing mission — consistency-with-existing-structure only becomes a constraint once a design is chosen and it's time to implement it in code.
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
| `Puzzle` | ESC returns to Exploration; pipe clicks route directly via `IPointerClickHandler` on each Canvas-based `PipeVisual`, not through `Tick()` polling |
| `BridgeBuilder` | Forwards press/hold/release world positions into `BridgeBuilderSystem.HandleDragStart/Update/End` — a press over any UI raycast target (`EventSystem.IsPointerOverGameObject()`) is skipped so HUD button clicks aren't also read as build gestures; ESC returns to Exploration (guarded — only while `Phase == Building`) |
| `RoutineBuilder` | ESC returns to Exploration; card drag/drop routes directly via Unity's `IBeginDragHandler`/`IDropHandler` on the Canvas panel, not through `Tick()` polling |
| `KanbanBuilder` | ESC returns to Exploration (guarded — only while not mid-simulation); threshold-marker dragging and the Run Day button route directly via Unity's own UGUI handlers on the Canvas panel, not through `Tick()` polling |
| `Reflection` | Delegates left-click to `ReflectionPopupUI.OnDismiss()` |
| `MissionBoard` | ESC returns to Exploration |
| `TownNotice` | Empty stub — the town notice panel (upgrades, rushed-fix breakdowns) is dismissed via a UI Button wired directly to `TownNoticeUI.OnDismiss()` in the Inspector, not through `Tick()` (was `DayComplete`; renamed in place so the enum's int value is unchanged) |
| `InfoBoard` | ESC returns to Exploration (same shape as `MissionBoard`) |

### Event Bus
`EventBus` is a static class of C# events. Systems subscribe in `OnEnable`/`OnDisable` and raise via the `Raise*` helpers. This is the only coupling layer between systems — no direct references across domains.

Key events: `OnMapClicked → OnPathRequested → OnPathGenerated`, `OnSolutionSelected`, `OnMissionCompleted`, `OnMissionsNeedReview`, `OnReflectionDismissed`, `OnTownUpgraded`, `OnInventoryChanged`, `OnTrustChanged`, `OnPDCAPhaseChanged`, `OnObjectiveProgress`.

### Mission Flow (complete happy path)
1. Player clicks NPC (`IInteractable.Interact()`) → `DialogueState`
2. `DialogueManager` exhausts all lines → opens `PlanningUI` → `PlanningState`
3. `PlanningUI` type-animates trivial solution name, then optimal solution name, then runs the 5 Whys quiz (see below), which determines the outcome — the player no longer manually picks trivial vs. optimal
4. On the 5th why, `PlanningUI` itself calls `RaiseSolutionSelected(missionID, type)` (5/5 correct → Optimal, anything less → Trivial) → `MinigameActivator` activates the right container and changes to its inspector-assigned `targetState`

(Advanced missions — `MissionData.isAdvancedMission`, e.g. Mission 5 — diverge starting at step 3: the quiz no longer picks the path. See 5 Whys Quiz below and Mission 5: Bridge Building.)
5. Puzzle solved / well patched → `RaiseMissionCompleted(missionID, wasOptimal)`
6. `ReflectionPopupUI` listens, shows feedback text from `MissionData`, changes state to `Reflection`
7. Player clicks to dismiss → `Exploration`; `MissionBoardUI` listens to grey out the entry

A trivial resolution isn't final — see Rushed-Fix Breakdown below: it breaks down and reopens after the player completes their next mission.

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
anything; reattempting a trivial mission is handled entirely by Rushed-Fix Breakdown below.
Trust persists for the whole game, since it's a standing relationship signal rather than
per-attempt bookkeeping. `NPCTrustUI` is a companion component
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

The quiz behaves differently on a redo (see Rushed-Fix Breakdown below): `hintText` only shows `WhyStage.hint` when `MissionReviewSystem.IsMissionUnderReview(missionID)` is true, so a first attempt gets no hint but a review redo does; and each stage's distractor pool excludes whatever the player already picked wrong on a prior attempt at that stage (`MissionReviewSystem.RecordWrongAnswer`/`GetExcludedDistractors`), with a floor guard so a question never collapses down to just the correct answer alone.

**Advanced missions** (`MissionData.isAdvancedMission`, e.g. Mission 5) don't use `correctCount` for path selection at all: `PlanningUI.SelectAdvancedMission()` runs instead of the block above, always raising `OnSolutionSelected(missionID, SolutionType.Optimal)` — a routing placeholder into that mission's single container, not a claim about the eventual result — then `OnFiveWhysCompleted(missionID, correctCount)` so the minigame itself can spend the score on something else (Mission 5 turns it into bonus test attempts; see Mission 5: Bridge Building). `OnSolutionSelected` has to fire first: it's what activates the until-now-inactive container, synchronously running that object's `OnEnable` before the next line executes — only after that does the container's own `OnFiveWhysCompleted` subscription actually exist.

### Mission Board
`MissionBoardUI` holds one `MissionEntryUI` per mission (assigned in Inspector). On `OnMissionCompleted`, the matching entry greys out (`alpha = 0.4`) and its status label reads "Resolved" (optimal) or "Needs Review" (trivial). On `OnSolutionSelected`, `NPCController.HandleSolutionSelected` sets an internal `missionCompleted` flag (so `Interact()` becomes a no-op) and hides its `InteractionIndicator`, but the NPC's GameObject itself stays active — it remains visible (and keeps patrolling, if it has an `NPCPatrol`) rather than disappearing.

A "Needs Review" (trivial) mission reopens only when its rushed fix breaks down (see Rushed-Fix Breakdown below) — there's no way to manually revisit it before then. Once `OnMissionsNeedReview` fires for it, `MissionEntryUI.ResetVisual()` un-greys the entry and the mission's own interactable/minigame resets itself so it can be replayed.

### Data Layer (ScriptableObjects)
- **`MissionData`** — all text content for one mission: complaint, root cause, 5 Whys quiz data (`fiveWhys: WhyStage[5]`, each with `question`/`correctAnswer`/`distractors[]`/`hint`), an optional `minigameHint` (shown only mid-minigame when `MissionReviewSystem.IsMissionUnderReview` is true — for advanced missions whose action-phase minigame isn't structured as discrete Why stages the way the quiz is, so there's no per-question `hint` slot to reuse; see Mission 3 below), solution names, reflection texts. Create via `Kaizen Systems/Mission Data`. `M1_ParchedCrops`, `M2_CleaningRiver`, `M3_BrokenRoutine`, `M4_KanBanMarket`, and `M5_BrokenBridge` have their 5 Whys chains populated, each one ending at the mission's `actualRootCause`.
- **`MissionRegistry`** — array of `MissionData`, looked up by `missionID`. Create via `Kaizen Systems/Mission Registry`. Assign in Inspector on `ReflectionPopupUI`, `MissionReviewSystem`, and `TownNoticeUI`.
- **`ItemData`** — one inventory item's `itemID`, `itemName`, `icon`, and stacking rules (`stackable`, `maxStack`). Create via `Kaizen Systems/Item Data`. Five assets exist: **Gold Coin** (stackable) and **Trash** (not stackable, so litter piles up one slot per piece), assigned in Inspector on `CoinRewardSystem`, `TrashPiece`, `TrashCollectionSite`, `TownUpgradeSystem`, and `InventorySystem.reservedSlotItem`; **Brick** (not stackable, only ever one needed) for Mission 1's trivial fetch quest, assigned on `BrickPickup` and `WellPatchSite`; **Machine Part** (stackable) and **Winch** (not stackable) for Mission 2's optimal path, assigned on `MachinePart`, `AssemblyPoint`, and `PlacementPoint` — `AssemblyPoint.Interact()` trades 3 Machine Parts for 1 Winch, and `PlacementPoint.Interact()` consumes the Winch on final placement.
- **`BridgeMaterialData`** — one bridge-plank material's `materialID`/`materialName`/`icon`, plus the actual cost-vs-strength tradeoff: `costPerUnitLength` (spent from `BridgeBuilderSystem.budget`), `breakForce` (that plank's `HingeJoint2D.breakForce`), `plankColor`, and `isRoad` (default true — false marks a reinforcement-only material, see Mission 5: Bridge Building for what that changes about collision). Create via `Kaizen Systems/Bridge Material Data`. No registry asset — low cardinality (2-3 materials), so `BridgeBuilderSystem.materials` just holds the array directly, referenced only within Mission 5's own minigame. See Mission 5: Bridge Building.

### Rushed-Fix Breakdown (Mission Review)
There are no stages or days — missions can be played in any order, and progress is the coin-funded
town upgrade (see Gold Coin Economy below). Stages (`StageManager`/`StageData`/`StageRegistry`: a
Town Hall submission gated on every mission in a batch being optimal *and* no trash on the ground)
were removed. Once coins moved to upgrades and missions became order-free, they were mostly extra
rules, and the no-trash gate felt like a bug whenever a piece spawned just as the player walked up
to submit.

What stages *did* provide — sending trivial fixes back — is now `MissionReviewSystem` (singleton;
`StageManager.cs` renamed in place, GUID kept, so the scene object carried over), and it happens in
the world instead of at a desk: **a trivial resolution is a rushed fix, and it breaks down after the
player completes their next mission** (any other mission, optimal or not). If the rushed mission is
the only one not yet optimal, there's no "next mission" to wait for, so it breaks down right away —
otherwise the game could never be finished. Rules, in `HandleMissionCompleted`:
- record `missionOutcomes[id]`;
- every *other* rushed fix still standing (`standingRushedFixes`) moves to `pendingBreakdowns`;
- if this completion was trivial: straight to `pendingBreakdowns` when every other
  `MissionRegistry` mission is optimal, otherwise into `standingRushedFixes`.

**Breakdowns are raised on `EventBus.OnReflectionDismissed`, not inside the `OnMissionCompleted`
dispatch.** Raising `OnMissionsNeedReview` mid-dispatch would race the completing mission's own
teardown (subscriber order across `MinigameActivator`/interactables disabling themselves isn't
guaranteed — e.g. a reset re-activating `RiverInteractable` before its own completion handler
deactivates it), and `TownNoticeUI` changing state would fight `ReflectionPopupUI`'s.
`ReflectionPopupUI.OnDismiss()` raises `OnReflectionDismissed` *after* its own
`ChangeState(Exploration)` — raising it first would let that Exploration change overwrite the
notice's `TownNotice` state.

**`OnMissionsNeedReview` reopens the broken-down missions in place.** Every mission-specific system listens for it and resets itself to pre-completion state: `NPCController` (Mission 1's NPC) clears `missionCompleted` and calls `InteractionIndicator.ResetVisibility()`; `RiverInteractable` (Mission 2's trigger) re-`SetActive(true)`s itself; `PipePuzzleSystem` resets every `PipeVisual` to its cached original rotation/bitmask (`ResetPuzzle()`); `PartCollectionSystem`/`WastePickupSystem` reset their collected counts and re-show their pieces (`MachinePart.ResetPart()`, `WastePiece.ResetPiece()`, `AssemblyPoint.ResetPoint()`, `PlacementPoint.ResetPoint()`); `MissionBoardUI`/`MissionEntryUI` un-grey the entry (`ResetVisual()`); `TownNoticeUI` announces it ("A Quick Fix Gave Way", naming the missions via `MissionData.missionName`). Components living inside a container `MinigameActivator` disables after mission completion (`PartCollectionSystem`, `WastePickupSystem`) or that disable themselves on completion (`RiverInteractable`, `PipePuzzleSystem`) subscribe to `OnMissionsNeedReview` in `Awake`/`OnDestroy` rather than `OnEnable`/`OnDisable`, since an `OnEnable`/`OnDisable` subscription would already be torn down by the time a review request — which can only happen after the mission is complete — needs to reach it.

**A redo runs through the same 5 Whys quiz** with hint/distractor differences from a first attempt — see 5 Whys Quiz above. `MissionReviewSystem` owns that scaffolding too (`IsMissionUnderReview`, `RecordWrongAnswer`/`GetExcludedDistractors`). Trivial completions never earned a Gold Coin, so there's nothing to retract on a breakdown.

### Pathfinding & Grid
- **`GridSystem`** (pure C#) — 2D array of `GridNode`; converts between world positions and grid coordinates.
- **`PathfindingSystem`** (MonoBehaviour) — builds a `GridSystem` in `Start`, reads a collision `Tilemap` to mark unwalkable cells, runs A* using a `NodeMinHeap` min-heap. Two entry points into the same A* core: the `EventBus.OnPathRequested` handler (`CalculatePath`) fires `RaisePathGenerated` with the result and is what `PlayerController` uses; `RequestPathSync(start, end)` returns the path directly instead of broadcasting it, for callers that must not go through the shared event pair (see NPC Patrol below — every subscriber receives every `OnPathGenerated`, so a second broadcaster would make the player walk an NPC's path or vice versa). `GetRandomWalkableCoordinates(Vector2Int from)` does a BFS flood-fill from `from` over walkable neighbors and returns a random cell from the reachable set, so callers never get handed a walkable "island" cell that's cut off by unwalkable tiles (which would otherwise burn a full failed A* search and log a warning). Exposes `SetWalkable(Vector3 worldPos, bool walkable)` to toggle grid cells at runtime (e.g. doors, mission triggers).
- **`InputManager`** — converts `OnMapClicked` world positions to grid coords; if an `IInteractable` is adjacent it calls `Interact()`, otherwise fires `RaisePathRequested`.
- **`PlayerController`** — listens to `OnPathGenerated`, walks the path via a coroutine, flips the `SpriteRenderer` on horizontal movement. Before each step checks the next cell via `Physics2D.OverlapPoint` against an `npcLayerMask`; if blocked, it `yield return null`s once before re-requesting the path from the current position, so the player reroutes around moving entities. That single-frame wait is load-bearing, not cosmetic: re-requesting synchronously can recurse into `StartCoroutine(FollowPath(...))` again within the same call stack, and if the newly computed path is blocked at its own first step too (e.g. a patrol NPC parked on the only route), it recurses without ever yielding and overflows the native stack.

### Mission 2: The Blocked River
The map's river now runs from a cliff-top source down a waterfall and on through the village,
jammed to a trickle below the falls — there's no pond; it's the river itself, blocked at its
source, that's gone stagnant. The interactable that starts the mission is `RiverInteractable`,
positioned on the boulder wedged at the lip of the falls (not an NPC) — a natural rockslide, not
litter or a human cause. Both solutions run inside `ExplorationState` — no new game states needed.
Two additional `ContextInteractable` points sit nearby (the thinned-out riverbed below the falls,
villagers complaining that the river has gone stagnant and unsafe to drink/wash in) purely to give
the player narrative context before they attempt the 5 Whys quiz — they show dialogue and return
straight to `Exploration`; they don't reference a `MissionData` or touch mission state at all.

**Trivial — Clear the Loose Rubble:** `MinigameActivator` activates `TrivialContainer`, which
holds `WastePickupSystem` and a set of `WastePiece` IInteractables overlapping loose rubble shaken
free by the rockslide (not litter). Each `WastePiece.Interact()` hides its paired `wasteVisual`
and calls `WastePickupSystem.OnWasteRemoved()`. When remaining count hits zero, fires
`RaiseMissionCompleted(2, false)` — enough rubble clears for a trickle, but the wedged boulder
itself stays put, so the river keeps stagnating.

**Optimal — Rig a Cliffside Winch:** `MinigameActivator` activates `OptimalContainer`, which holds
`PartCollectionSystem` and 3 `MachinePart` IInteractables placed at fixed positions in the editor.
Each `MachinePart.Interact()` collects itself and calls `PartCollectionSystem.OnPartCollected()`.
At 3/3, `PartCollectionSystem` activates `AssemblyPoint` near the cliff lip. `AssemblyPoint.Interact()`
shows the assembled winch visual and activates `PlacementPoint` at the falls. `PlacementPoint.Interact()`
shows the anchored winch visual and fires `RaiseMissionCompleted(2, true)` — the winch levers the
boulder free and stays bolted in place to catch whatever comes down next.

**River reveal:** `RiverManager` listens to `OnMissionCompleted` for missionID 2. On either
solution: disables `blockageVisual` (the wedged boulder), enables `animatedRiverTilemap` — the
falls resume flowing downstream either way, since the visual payoff is identical regardless of
path; only the reflection text (and whether a future slide gets caught automatically) differs.

### Mission 1: Well & Pipe Puzzle
Like Mission 2's river, the well itself (not an NPC) is the interactable that starts the
mission — `NPCController` (`Assets/Scripts/Core/Missions/NPCController.cs` — lives at the shared
`Missions/` root, not under `Mission1/`, since it's a generic dialogue-trigger component reused
by other missions, not mission-specific) is attached directly to the well's `GameObject` rather
than a separate Farmer NPC.
Since two things now need to be clickable at the same world position (the dialogue trigger,
then whatever the chosen path activates there), `HandleSolutionSelected` disables the well's
own `Collider2D` once a solution is picked — `Physics2D.OverlapPoint` doesn't guarantee which
of two perfectly-overlapping colliders it returns, so leaving both live would make clicks land
on the wrong one unpredictably. `HandleMissionsNeedReview` re-enables it for a breakdown redo.

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
rotation works. Clicks delegate to `PipePuzzleSystem.RotatePipeAt` via `PipeVisual.OnPointerClick`
(`IPointerClickHandler`, resolved by Unity's own `EventSystem`/`GraphicRaycaster` — no adjacency
requirement either way, a precise click on a pipe tile rotates it regardless of player position).
The puzzle system runs a DFS flood-fill from
`startPos` to `endPos` to check for a valid water path after every rotation, and raises
`RaiseMissionCompleted(id, true)` once solved. The puzzle board is a full 5×5 grid (all four
`PipeShape`s in play — `Straight`, `Corner`, `TJunction`, `Cross`); not every cell needs a pipe
(`PipePuzzleSystem` only populates grid cells where a `PipeVisual` actually exists — an
unfilled cell is just `null` and the flood-fill skips it).

`Container_Optimal_M1` is a Canvas panel — `Canvas`/`CanvasScaler`/`GraphicRaycaster` directly on
the container itself (same collapsed shape Mission 3 uses, see below), not a world-space
`SpriteRenderer` grid with a `CameraFollower` the way it used to be. `PipeVisual` is `Image`-based
now, and click resolution moved from a broadcast `EventBus.OnPuzzleClicked` world position + each
pipe's own `Collider2D.OverlapPoint` check to the `IPointerClickHandler` above. The conversion only
ever touched the rendering/input layer: `GetStartingBits()`'s canonical-bits-per-shape calibration
above and `PipePuzzleSystem`'s flood-fill are both untouched, since neither ever depended on
`SpriteRenderer` specifically — `GetStartingBits()` only reads `transform.eulerAngles.z`, which an
`Image`'s `RectTransform` reports identically to a world-space `Transform`.

### Mission 3: The Farmer's Broken Routine

Like Mission 1's well and Mission 2's boulder, the thing that starts this mission isn't an NPC —
it's `RoutineBoardInteractable`, on a schedule board prop in the world near the farm. Mission 3 is
this game's second **Advanced Mission** (`MissionData.isAdvancedMission`, see 5 Whys Quiz above),
following the shape Mission 5 established: no separate trivial-path container, the 5 Whys quiz
doesn't pick `SolutionType` at all — `PlanningUI.SelectAdvancedMission()` always routes into the
single `Container_Optimal_M3`, and that container's own minigame simulation decides `wasOptimal`
directly.

**The minigame (`FarmRoutineSystem`) is a drag-to-reorder puzzle:** 4 station cards
(`RoutineCardUI` — "Feed the Animals", "Water the Crops", "Harvest the Crops", "Sell at Market")
sit in 4 fixed `RoutineSlotUI` positions; the player drags cards between slots to propose an
order, then clicks Submit. `acceptedOrders` holds the 2 permutations the design doc's "2 desirable
outcomes" calls for — `[Feed, Water, Harvest, Sell]` and `[Water, Feed, Harvest, Sell]` — since
feeding the animals and watering the crops don't depend on each other and can go in either order,
but both have to happen before the harvest, and the harvest has to happen before anything can be
sold. Station identity is purely an array index (`stations[i]`/`cards[i]` authored 1:1 in
parallel, same fixed-array-authored-in-parallel shape as `PlanningUI.fiveWChoiceButtons`/
`BridgeBuilderUI.materialButtons`) — `Initialize(i, ...)` assigns that identity at `Awake`, so it
doesn't matter which physical card GameObject ends up at which array index; whichever one does
just gets labeled and identified as that station.

**A Canvas is the whole container — not a Canvas plus a separate container the way Mission 5
has.** Mission 5 splits `BridgeCanvas` (screen-space UI: budget/status/buttons) from
`Container_Optimal_M5` (a world-space `Rigidbody2D` physics playground) because those are two
fundamentally different rendering systems that can't share a hierarchy branch. Mission 3 has no
such mixing: cards, slots, text, and the Submit button are all UI, so `Container_Optimal_M3`
carries `Canvas`/`CanvasScaler`/`GraphicRaycaster`, `FarmRoutineSystem`, and `RoutineBuilderUI`
directly on one GameObject — same as every other mission's container is one object — not nested
under `PlayerCanvas` either. Dragging resolves through Unity's own `IBeginDragHandler`/
`IDragHandler`/`IEndDragHandler`/`IDropHandler` (`RoutineCardUI`/`RoutineSlotUI`) rather than
hand-rolled hit-testing — `FarmRoutineSystem` only reparents whatever card/slot the raycaster
already resolved (`HandleCardDropped`/`PlaceCardInSlot`), it never does its own
`Physics2D`/`RectTransform` overlap math. Mission 1's pipe puzzle went through this exact same
Canvas conversion for the same reason — see above.

**Attempts** (`baseAttempts`, default 5, matching the design doc's "5 tries for the player"
directly) **plus bonus attempts from the 5 Whys score** (`bonusAttemptsPerCorrectWhy`, same idea as
`BridgeBuilderSystem.bonusAttemptsPerCorrectWhy` — a strong diagnosis earns extra room to get the
actual arrangement right, even though this mission's quiz doesn't pick the path either).
Submitting a wrong order doesn't end the attempt loop immediately — `FarmRoutineSystem` increments
`attemptsUsed` and lets the player try again until attempts run out, at which point
`RaiseMissionCompleted(3, false)` fires (the trivial outcome, same "exhausted attempts" shape
`BridgeBuilderSystem.HandleTestFailed` uses). `MissionData.minigameHint` is shown only when
`MissionReviewSystem.IsMissionUnderReview(3)` is true (the design doc's "hint on the second attempt"),
same review-only-hint convention as `WhyStage.hint`.

**The per-submit "small CG" the design doc calls for — an NPC visibly executing the tasks in
order — is still under construction, not built yet.** `FarmRoutineSystem.SimulateAndResolve()`
steps through the submitted order calling `RoutineCardUI.PlayStepHighlight()` on each card in turn
(a brief color pulse on that card's background), which stands in for the real payoff without
requiring any animation work. Swapping in an actual animated CG later is a drop-in replacement at
that one call site — nothing about the ordering/evaluation logic depends on how that step is
visualized.

### Mission 4: The Tangled Marketplace

Like Mission 1's well, the trigger is an `NPCController` (`Assets/Scripts/Core/Missions/NPCController.cs`
— the shared, generic dialogue-trigger component, not a mission-specific script) on a `Merchant_NPC`
GameObject, rather than a broken prop in the world. Mission 4 is this game's third **Advanced
Mission** (`MissionData.isAdvancedMission`, see 5 Whys Quiz above): no separate trivial-path
container, and the 5 Whys quiz doesn't pick `SolutionType` at all — `PlanningUI.SelectAdvancedMission()`
always routes into the single `M4KanbanPanel` container, and that container's own simulation
decides `wasOptimal` directly, with `MinigameActivator.singleContainerForMission` checked so the
one container closes correctly on either outcome, same as Mission 3/5.

**Design history — Mission 4 was originally built as a classic mission** (quiz picks `SolutionType`
directly, two separate containers: a "Restock by Feel" trivial container and this Kanban container
as the optimal path), then converted to Advanced mid-development for two compounding reasons: (1)
the same reasoning Mission 3/5 are built on — a multiple-choice diagnosis can't stand in for "did
you actually tune the thresholds correctly" any better than it can for "does the bridge hold," so
the simulated day's own pass/fail is the more honest test; and (2) the old trivial path
(`MarketStallTrivialSystem`/`MarketStall`, still in the codebase) ran a real-time day timer gated
only on `GameStateType.Exploration` — a player who wandered off to explore the rest of town (still
valid `Exploration`) could have the mission silently resolve trivially without ever being near the
market to see it happen, the only mission in the game with that failure mode. A later pass added
player-proximity gating (`MarketStallTrivialSystem.marketRadius`) as a partial fix before the
Advanced-mission conversion removed the background clock entirely. The retired scripts are kept
IInteractable-ready (see above) for reuse in the planned post-5-missions farming/market sandbox —
see `Docs/TODO.md`.

**The minigame (`KanbanBuilderSystem`) is a configure-then-simulate puzzle** — a new mechanical
genre for this game (every other optimal minigame is either manipulated continuously or a discrete
fetch/assemble/place chain). Four `StallConfig` entries (`maxStock`/`consumptionRate`/
`deliveryLeadTime`/`wastefulThresholdRatio`), each paired 1:1 by array index with a
`KanbanStallGaugeUI` (same fixed-array-authored-in-parallel shape as
`FarmRoutineSystem.stations`/`cards`). The player drags each gauge's reorder-point marker, then
presses Run Day: `SimulateDay()` drains each stall at its `consumptionRate`, triggers a delivery
once stock crosses the player's threshold, and lands that delivery after `deliveryLeadTime` —
consumption keeps draining during the wait, so a threshold set below
`consumptionRate * deliveryLeadTime / maxStock` guarantees a stockout. A threshold set above
`wastefulThresholdRatio` fails on principle from the start of the day, regardless of what happens
afterward — "always topped up" isn't pulling on demand. Both failure modes are tracked per stall as
a `FailureReason` enum (`Stockout`/`Wasteful`, not a single bool) specifically so the end-of-day
message can tell the player which direction to adjust each failing stall, not just that it failed.
`SetStockRatio` always shows the honest live stock color — it never force-colors a stall red just
because it's flagged failed, since that made a "wasteful" stall sitting at 90% full look identical
to one actually about to run dry; a dedicated `FailureOutline` (toggled once at day's end) is the
one visual guaranteed to show up for either failure type. Live event callouts
("Reordered!"/"Restocked!"/"Ran dry!") fire at the exact simulation-state transitions, narrating
what would otherwise be silent bar motion.

**Attempts** (`baseAttempts`, default 5) **plus bonus attempts from the 5 Whys score**
(`bonusAttemptsPerCorrectWhy`, same idea as `FarmRoutineSystem`/`BridgeBuilderSystem`) — a failed
day increments `attemptsUsed` and lets the player retune and retry; exhausting `MaxAttempts`
without passing fires `RaiseMissionCompleted(4, false)` (same "exhausted attempts" shape
`BridgeBuilderSystem.HandleTestFailed`/`FarmRoutineSystem` use).

**`KanbanStallGaugeUI`'s drag math is rotation-agnostic by necessity**: the gauge prefab's source
sprite is horizontal, so the authored `Track` is rotated (typically 90°) to read as a vertical bar.
Reading `track`'s local Y directly (as a naive vertical-slider implementation would) breaks the
instant the object is rotated, since a rotated rect's local axes no longer correspond to "up" on
screen. The fix: `OnDrag` determines which of `Width`/`Height` is actually the bar's long dimension
(rotating a Transform doesn't swap its own numeric Width/Height), builds the two local extremes
along that axis, and carries them through whatever rotation is currently applied via
`track.TransformPoint(...)` before comparing against the pointer in screen space — correct
regardless of rotation angle or direction, rather than assuming a specific one.

**`KanbanBuilderState`** is ESC-only outside of a running simulation (same `canLeave` guard shape
as `RoutineBuilderState`/`BridgeBuilderState`) — all actual input (dragging thresholds, the Run Day
button) resolves through Unity's own UGUI handlers, not `Tick()` polling.

**`MarketAmbientSystem` is a permanent, post-completion epilogue** — entirely separate from
`M4KanbanPanel`, listening for `OnMissionCompleted` for missionID 4 the same way `RiverManager`
listens for Mission 2's, except driving an ongoing simulation instead of a one-time visual flip. It
owns its own permanent, collider-less `MarketStall` instances (decorative — `InputManager` can't
detect a `IInteractable` with no `Collider2D`) and locks into one of two modes forever based on
`wasOptimal`: **"Unmanaged"** (trivial outcome) dispatches a `MarketAttendantNPC` to a random stall
on a random timer, completely decoupled from actual stock level; **"Kanban"** (optimal outcome)
continuously checks each stall's live `MarketStall.StockRatio` against
`KanbanBuilderSystem.GetThresholdRatio(i)` — the player's own tuned value, read back even though
the container is by then inactive, since `SetActive(false)` doesn't clear a component's fields —
and dispatches the instant it crosses. `MarketAttendantNPC` reuses `NPCPatrol`'s exact movement
shape (`PathfindingSystem.RequestPathSync` + step-along-path) but is dispatched to a specific
destination on demand rather than wandering randomly; the walk itself stands in for delivery lead
time, deliberately not a separate abstract timer.

### Mission 5: Bridge Building (Full Poly Bridge)

Like Missions 1 and 2, the broken thing itself — not an NPC — is what starts the mission:
`BridgeInteractable` sits on the bridge GameObject in the world (same role as `RiverInteractable`
on the boulder). Unlike Missions 1 and 2, Mission 5 is an **advanced mission**
(`MissionData.isAdvancedMission` — see 5 Whys Quiz above): there is no separate trivial-path
container, and the 5 Whys quiz doesn't pick `SolutionType` at all — `PlanningUI` always routes
into the single `Container_Optimal_M5`, spending the quiz score as bonus test attempts instead.
The container's own build/test result decides `wasOptimal` directly: a passed test raises
`RaiseMissionCompleted(id, true)`; running out of test attempts raises `RaiseMissionCompleted(id,
false)` — the "quick, unreinforced bridge" outcome — with no separate fetch-quest ever played.
`MinigameActivator.singleContainerForMission` is what lets this one container close correctly on
either outcome, since the usual solutionType-vs-wasOptimal match can't otherwise tell whether it's
"the" container that resolved the mission (it's the only one, always).

**The minigame itself (`BridgeBuilderSystem`) is a full Poly Bridge**, not a simplified one — the
player freely places joints and beams within a cost budget, rather than connecting a fixed,
Editor-authored grid:
- **Free placement.** Only anchor nodes (`BridgeNode.isAnchor`) are Editor-authored; every deck
  node is created at runtime by a drag (`HandleDragStart/Update/End`) that resolves each end to
  either an already-existing point (`FindNearestNode`, a plain distance scan over the small
  tracked node list — no `Collider2D`/physics query needed) or a snapped, brand-new one
  (`SnapToGrid`, clamped to `playgroundBounds`) — never both new in one drag; at least one end
  must already exist. Node/beam resolution is centralized here rather than decentralized like
  `PipeVisual`/the old fixed-grid `BridgeNode` — grid-snap resolution ("nearest point within
  radius, existing node or empty space") isn't something any single node's own collider could
  ever answer about itself, so `BridgeNode` carries no collider or click-handling of its own, and
  there's no `EventBus.OnBridgeClicked` the way there's an `OnPuzzleClicked`.
- **Materials.** `BridgeMaterialData` is cost vs. strength per plank (see Data Layer above). The
  player picks one via `BridgeBuilderSystem.SelectMaterial(index)` before dragging; it's baked
  into the plank's cost/`breakForce`/color at placement (`BridgePlank.Setup`) and never changes
  afterward. `isRoad` (default true) is a second, independent axis — a reinforcement material
  (`isRoad == false`) fully participates in `HingeJoint2D` load-bearing exactly like a road
  material does, but `PlacePlankInternal` puts its plank on a separate `bridgeSupportLayerName`
  layer (`BridgeSupport`) instead of `bridgeLayerName` (`Bridge`, where the cart itself lives),
  and the Physics 2D Layer Collision Matrix is configured so those two layers never collide —
  collision detection and joint constraint solving are separate Unity systems entirely, so a
  reinforcement beam can brace a span without the cart's own collider ever touching it, the same
  distinction real Poly Bridge draws between road and every other material.
- **Stress visualization.** During the Testing phase, `Update()` drives
  `BridgePlank.UpdateStressVisual()` on every placed plank each frame — it color-lerps from its
  own material color toward a shared `breakingColor` (red) based on
  `max(jointA, jointB).reactionForce.magnitude / breakForce`, a cheap read of the physics
  engine's already-solved joint state. A joint that actually breaks gets destroyed by Unity right
  after `OnJointBreak2D` fires — the plank's GameObject survives until the next `ResetBridge()`,
  so `Update()` keeps calling this every frame in the meantime — so `jointA`/`jointB` are
  null-checked (Unity's `== null` correctly reports true for an already-destroyed `Object`) rather
  than read unconditionally; a broken joint counts as maximum stress, not "skip this plank".
  Above `flashThreshold` (default 0.8) the plank also blinks to `flashColor` — a second,
  non-color cue, since the red lerp alone is hard to read with red-green color vision deficiency.
- **Current materials** are Wood (cheap/weak), Road (mid, the only `isRoad` one), and Steel
  (expensive/strong, reinforcement-only). Steel was originally named "Cable" and renamed because
  every material is a rigid `HingeJoint2D` plank — it resists compression too, which a real cable
  can't, and teaching that wrong in an educational game was worse than the less-exotic name. A
  true tension-only cable would need a different joint (e.g. `DistanceJoint2D` with
  `maxDistanceOnly`), not just a new `BridgeMaterialData`. `plankColor` *multiplies* the plank
  sprite, so the plank sprite itself must be white/light — a dark sprite makes every material
  read as near-black.
- **Grid overlay.** `BridgeGridOverlay` (a `SpriteRenderer` child on the `Bridge` layer, wired to
  `BridgeBuilderSystem.gridOverlay`) draws a faint dot at every `SnapToGrid` point while
  building, refit from `ComputePlaygroundBounds()` and hidden for the test. Its dot sprite is
  generated in code (`Sprite.Create`, `FullRect` mesh for `SpriteDrawMode.Tiled`) with PPU
  derived from `gridSpacing`, so one tile always equals one snap step — an imported sprite's PPU
  would have to be kept in sync by hand. Its `SpriteRenderer` is a lazy getter for the same
  cross-object `Awake`/`OnEnable` ordering reason as `BridgeTestCart.Rb`.
- **HUD (`BridgeBuilderUI`).** Budget reads "spent / total"; `attemptsText` is its own label
  beside Test rather than trailing the hint; `statusText` only reports the live phase (empty
  while building, "Testing..." during a test) — the objective/how-to hint is static,
  Editor-typed text on a separate TMP object that the script doesn't reference at all. Material
  buttons get their name plus cost/strength from `BridgeMaterialData` in `Start()`, and the
  selected one is tinted (`selectedMaterialTint`) every frame.
- **Sorting / click-blocking against `PlayerCanvas`.** `PlayerCanvas` is Screen Space - Camera on
  the `UI` sorting layer at order 0, so it sorts *with* the container's world sprites: anything
  in the container must sit above order 0 to cover the HUD (the `BackGround` parchment tied at 0
  and drew under it). Order within the container: background → grid → planks (15) → nodes (16,
  so they stay grabbable on top of beams) → cart (20). The HUD is still there underneath,
  though, and still raycastable — since the press guard above treats any raycast target as UI,
  every non-interactive `PlayerCanvas` graphic (mission directory lines, PDCA text, inventory
  images, minimap) must have **Raycast Target** unticked, or it silently blocks building
  wherever it sits (found via the `Mission4` directory line swallowing presses on the start
  anchor). For TMP, the toggle is under the component's collapsed **Extra Settings**.
- **Undo/redo.** `BridgeActions.cs` defines `IBridgeAction` (`PlaceBeamAction`, `DeleteNodeAction`)
  — small reversible commands on two `Stack<IBridgeAction>` (`Undo`/`Redo`), cleared on every
  `ResetBridge()` — a stale entry referencing an already-destroyed node/plank would corrupt state
  otherwise, and since `HandleTestFailed` already routes through `ResetBridge()`, "don't persist
  history across a failed test" falls out for free rather than needing separate handling. A
  recreated node's index is always reused verbatim on redo, never reallocated, so a later action's
  captured index can't go stale across an undo/redo cycle.
- **Node deletion** is a dedicated action, not overloaded onto the drag gesture: a press+release
  below a small movement threshold is treated as a click (selects the node) rather than a
  placement attempt; Delete/Backspace or the Delete button then removes the selected node,
  cascading to every plank touching it (refunding each one's stored `Cost`) — anchors can't be
  deleted.
- **The test cart** (`BridgeTestCart`) is deliberately dumb — plain constant-velocity locomotion
  (`FixedUpdate` forces `linearVelocity.x` back to `driveSpeed` every step) rather than full wheel
  physics, so it needs `Rigidbody2D` → `Freeze Rotation Z` and a `BoxCollider2D` (not
  `CircleCollider2D`) to rest flush on a plank without spinning under contact friction — a
  flat-bottomed body dragged at an externally-forced velocity has no stable
  rolling-without-slipping configuration otherwise. Its `Rigidbody2D` reference is a lazy-fetched
  property, not an `Awake`-only cache: `BridgeBuilderSystem.OnEnable()` (on the container root)
  calls `ResetToStart` via `ResetBridge()` synchronously during the same `container.SetActive(true)`
  that activates this object too, but Unity only guarantees an object's own `Awake` precedes its
  own `OnEnable` — never one object's `Awake` before a *different* object's `OnEnable`, even
  parent/child activated together — so caching only in `Awake` intermittently threw a
  `NullReferenceException` depending on which order Unity happened to run things in that frame.

**Presented as a popup, but for a different reason than Mission 1's pipe puzzle or Mission 3's
routine builder reach the same look.** Both of those are pure UI now — `Container_Optimal_M1`/`M3`
are Canvas panels, authored anywhere convenient in the scene, since a Canvas panel presents
identically regardless of its own GameObject's position (there's no "where in the world is this"
question for screen-space UI). `Container_Optimal_M5` can't take that shortcut: it's nothing *but*
`Rigidbody2D`-driven objects (nodes, planks, the cart) — Unity does not carry a moving non-physics
parent's motion into a `Rigidbody2D` child (the child's transform gets corrected back to hold its
world position) — so it has to stay world-space. That means, unlike M1/M3's Canvas panels,
`Container_Optimal_M5`'s own position (and everything nested inside it, including
`bridgeViewCamera`) has to be deliberately authored to coincide with wherever `BridgeInteractable`
(the real bridge the player walks up to) actually sits — a mismatch here is a real
Editor-authoring bug, not just cosmetic — found and fixed once already, when the
container had been left at a Mission-group's local origin rather than the bridge's actual
position, producing a jarring camera jump on every mission start instead of the popup simply
appearing where the player already was standing.
`BridgeBuilderState.Enter()`/`Exit()` instead
swap between the scene's normal player-tracking Cinemachine camera and a second, dedicated one
(`BridgeBuilderSystem.playerCamera`/`.bridgeViewCamera`) framing that fixed spot, handing tracking
back on Exit. `bridgeViewCamera` starts **inactive** in the Editor-authored hierarchy (only
`playerCamera` is active at rest) — `CinemachineBrain` drives Main Camera to match whichever
`CinemachineCamera` is active live in the Editor, not just Play mode, so leaving both
simultaneously active would make Edit-mode camera preview ambiguous/jumpy; `Enter()`'s explicit
`SetActive(true)` doesn't care what the object's authored starting state was, so this costs
nothing at runtime. Because the container stays active for the entire build/test session — only
deactivated on actual mission completion, not on an Esc-out mid-build — everything under it
(anchors, the node/plank prefabs, the cart, the preview line, the background sprite) sits on a
dedicated `Bridge` layer (a genuinely free/editable slot — Unity locks the names of layers 0, 1,
2, 4, and 5 even though slots 3, 6, and 7 sit in that same reserved 0-7 range and remain editable,
same as how `NPC` got its own layer earlier in the project), excluded from Main Camera's and
`MinimapCamera`'s Culling Mask *by default*, so the playground can't leak into normal Exploration.
Reinforcement-only planks (`BridgeMaterialData.isRoad == false`, see above) sit on a second such
layer, `BridgeSupport` (slot 7, the next free one) — excluded from both cameras the same way, and
additionally configured in the Physics 2D Layer Collision Matrix to never collide with `Bridge`
(where the cart itself lives), which is the actual mechanism that keeps the cart from touching a
reinforcement beam regardless of where it's placed.
Both layers' visibility has to be toggled at runtime, not left static, because a `CinemachineCamera`
(like `bridgeViewCamera`) has no Culling Mask of its own — Cinemachine 3 only blends
position/rotation/lens into the one real `Camera` (Main Camera), never the Culling Mask (verified:
Cinemachine 3 replaced its old per-vcam layer filtering with an unrelated Channels system
specifically because it moved away from Unity layers/culling for this). A permanent static
exclusion would hide them even while `bridgeViewCamera` is supposed to be showing them, so
`BridgeBuilderSystem.ShowBridgeLayers()`/`HideBridgeLayers()` flip both bits directly on
`Camera.main.cullingMask` from `BridgeBuilderState.Enter()`/`Exit()`, alongside the camera-active
swap. `MinimapCamera` needs no such toggle — it's a real `Camera` with its own Culling Mask, and
never needs to show the playground in any state, so its exclusion stays a permanent Editor setting.

### Singletons
`GameManager`, `DialogueManager`, `PlanningUI`, `MissionBoardUI`, `ReflectionPopupUI`, `TownNoticeUI`, `InventorySystem`, `TrustSystem`, `InfoBoardUI`, `MissionReviewSystem`, `TownUpgradeSystem`, `TrashSpawner`, `BridgeBuilderSystem`, `FarmRoutineSystem`, `KanbanBuilderSystem` all follow the same pattern: static `Instance`, destroyed if a duplicate exists in `Awake`.

### IInteractable
`NPCController`, `MissionBoardInteractable`, `RiverInteractable`, `WastePiece`, `MachinePart`, `AssemblyPoint`, `PlacementPoint`, `TrashPiece`, `TrashCollectionSite`, `TownHallInteractable`, `ContextInteractable`, `BrickPickup`, `WellPatchSite`, `InfoBoardInteractable`, `BridgeInteractable`, `RoutineBoardInteractable`, and `MarketStall` all implement `IInteractable`. (`MarketStall`'s own `Interact()` currently goes unused in Mission 4's Do phase — see Mission 4 below — but the component is kept IInteractable-ready for the planned post-5-missions farming/market sandbox, see `Docs/TODO.md`.) `InputManager` detects them via `Physics2D.OverlapPoint` and calls `Interact()` when the player is within 1 grid cell (or routes the player adjacent first). `ContextInteractable` is the odd one out: it's narrative-only (dialogue with no associated `MissionData`), so `DialogueManager` returns straight to `Exploration` afterward instead of opening `PlanningUI` — it never starts or resolves a mission.

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
nothing fits. Slot 0 is reserved for `reservedSlotItem` (the Gold Coin): nothing else may claim
it, and that item only ever goes there — coins are the town's upgrade budget, so one must never be
lost because trash filled every slot at the moment a mission awarded it. `CountItem`, `TryRemoveItem`, and `RemoveAllOfItem` round out the API. Every
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
**not** stackable, so every piece claims its own slot — letting litter pile up crowds out
mission items, never Gold Coins, which have the reserved slot); on success it removes itself from the spawner's occupied set and destroys
its GameObject exactly as before, on failure (inventory full) it's left on the ground untouched.
**`TrashCollectionSite`** is a plain `IInteractable` (same shape as `WellPatchSite`/
`RiverInteractable`) placed in the village — one interact calls
`InventorySystem.RemoveAllOfItem(trashItem)`, clearing every trash slot at once. Trash gates
nothing — its cost is purely inventory pressure on the fetch/collect missions.

**Spending coins — town upgrades.** `TownUpgradeSystem` (singleton) holds the town's
`CurrentLevel` (0 = rundown) and `upgradeCosts` (`{3, 2}` — all 5 coins in the game). Coins used to
be a 2-coin stage-submission fee, removed because it was redundant (coins only come from optimal
fixes and submission already required all-optimal) and so never asked the player anything. Buying
the final upgrade is the game's ending (all 5 coins = every mission optimal).
`PurchaseNextUpgrade()` (called only from `TownHallInteractable`, throws if unaffordable) removes
the coins, increments the level, and raises `EventBus.OnTownUpgraded(level)`. Everything that
reacts listens to that event rather than referencing the system: every `BuildingUpgrade` swaps
tier, and `TrashSpawner` scales its spawn interval by `intervalMultiplierPerTownLevel[level]`
(`{1, 2, 4}`) and re-rolls immediately so the effect starts right away. Tier 2 is only affordable
once every mission is optimal, so its trash effect is intentionally a post-missions reward.

### Town Hall & Town Upgrade Tiers
`TownHallInteractable` is purely the upgrade shop: if `TownUpgradeSystem.IsMaxLevel`, it shows
`villageCompleteLines`; if the next upgrade isn't affordable, `notEnoughCoinsLines` (each line run
through `string.Format` with `CoinsNeededForNextUpgrade` as `{0}`); otherwise it calls
`PurchaseNextUpgrade()` and shows no dialogue of its own — `TownNoticeUI` announces the upgrade.

`TownNoticeUI` (was `DayCompleteUI`; file + `.meta` renamed, GUID kept) is the full-screen panel
for both town-wide moments: `OnTownUpgraded` (title/body from `upgradeNotices[level - 1]`, the last
being the ending) and `OnMissionsNeedReview` (breakdown notice). Both change state to
`GameStateType.TownNotice`. `AudioManager` plays `townUpgradedClip`
(`[FormerlySerializedAs("dayCompletedClip")]`) on `OnTownUpgraded`.

`BuildingUpgrade` goes on every upgradable building and listens to `OnTownUpgraded(int level)`, activating the matching child in its `tiers` array (0 = rundown, 1 = improved, 2 = well built) and deactivating the rest; it also applies `TownUpgradeSystem.Instance.CurrentLevel` in `Start()` (safe — every `Awake` has run by then, same reasoning as `NPCTrustUI`), so a building's Editor-authored active child doesn't matter. It throws if the level exceeds its tier count rather than silently showing nothing. It replaced `TownHallUpgrade` (file + `.meta` renamed, GUID kept, `[FormerlySerializedAs("stages")]` preserves the old array), which only covered the town hall and indexed by *day* — the old stage system started at day 1 and incremented before raising, so the first stage pass raised day 2 and skipped the middle tier. Each tier child is a multi-child SpriteRenderer GameObject (not tilemaps) so it can have a Base sprite (EntityTilemap sorting layer) and a Roof sprite (ForeGroundTilemap sorting layer) to preserve player depth layering.

There is no day event any more (`OnDayCompleted` was removed with stages); `OnTownUpgraded(int level)` is the hook for anything that should react to town progress.
