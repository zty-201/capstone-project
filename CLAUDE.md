# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6 2D educational game built around Kaizen/PDCA methodology. Players explore a village, talk to NPCs with problems, choose between a trivial or optimal solution, complete a minigame, and receive reflective feedback. All game scripts live under `Assets/Scripts/`.

## Development

Open the project in the Unity Editor (Unity 6). There are no CLI build or test commands — all iteration happens inside the Editor. Scripts are compiled automatically on save.

## Coding Conventions

- Don't overengineer: Simple beats complex
- No fallbacks: One correct path, no alternatives
- One way: One way to do things, not many
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

Key events: `OnMapClicked → OnPathRequested → OnPathGenerated`, `OnSolutionSelected`, `OnMissionCompleted`, `OnDayCompleted`, `OnSatisfactionChanged`.

### Mission Flow (complete happy path)
1. Player clicks NPC (`IInteractable.Interact()`) → `DialogueState`
2. `DialogueManager` exhausts all lines → opens `PlanningUI` → `PlanningState`
3. `PlanningUI` type-animates trivial solution name, then optimal solution name, then runs the 5 Whys quiz (see below), which determines the outcome — the player no longer manually picks trivial vs. optimal
4. On the 5th why, `PlanningUI` itself calls `RaiseSolutionSelected(missionID, type)` (5/5 correct → Optimal, anything less → Trivial) → `MinigameActivator` activates the right container and changes to its inspector-assigned `targetState`
5. Puzzle solved / well patched → `RaiseMissionCompleted(missionID, wasOptimal)`
6. `ReflectionPopupUI` listens, shows feedback text from `MissionData`, changes state to `Reflection`
7. Player clicks to dismiss → `Exploration`; `MissionBoardUI` listens to grey out the entry

### 5 Whys Quiz (PlanningUI)
After typing both solution names, `PlanningUI` runs 5 sequential "Why" stages sourced from `MissionData.fiveWhys` (a `WhyStage[5]`, each with `question`, `correctAnswer`, `distractors[]`, `hint`). Picking any option always advances to the next stage — there is no blocking retry on a wrong pick — but `PlanningUI` tallies `correctCount` across the 5 stages. After the last stage, `outcomeIsOptimal = correctCount >= 5`, and that's what gets passed to `RaiseSolutionSelected`; hitting all 5 is intentionally hard, so each stage optionally shows `WhyStage.hint` via a subtle `hintText` element to nudge the player toward the correct answer without giving it away.

### Mission Board
`MissionBoardUI` holds one `MissionEntryUI` per mission (assigned in Inspector). On `OnMissionCompleted`, the matching entry greys out (`alpha = 0.4`) and its status label reads "Resolved" (optimal) or "Needs Review" (trivial). Note: this label does not currently imply a working revisit flow — on `OnSolutionSelected`, `NPCController.HandleSolutionSelected` sets an internal `missionCompleted` flag (so `Interact()` becomes a no-op) and hides its `InteractionIndicator`, but the NPC's GameObject itself stays active — it remains visible (and keeps patrolling, if it has an `NPCPatrol`) rather than disappearing. So a "Needs Review" (trivial) mission still cannot currently be reopened in-game, it's just that the NPC no longer vanishes when it's reached that state.

### Data Layer (ScriptableObjects)
- **`MissionData`** — all text content for one mission: complaint, root cause, 5 Whys quiz data (`fiveWhys: WhyStage[5]`, each with `question`/`correctAnswer`/`distractors[]`/`hint`), solution names, reflection texts. Create via `Kaizen Systems/Mission Data`. `M1_ParchedCrops` and `M2_CleaningRiver` have their 5 Whys chains populated, each one ending at the mission's `actualRootCause`.
- **`MissionRegistry`** — array of `MissionData`, looked up by `missionID`. Create via `Kaizen Systems/Mission Registry`. Assign in Inspector on `ReflectionPopupUI`.

### Pathfinding & Grid
- **`GridSystem`** (pure C#) — 2D array of `GridNode`; converts between world positions and grid coordinates.
- **`PathfindingSystem`** (MonoBehaviour) — builds a `GridSystem` in `Start`, reads a collision `Tilemap` to mark unwalkable cells, runs A* using a `NodeMinHeap` min-heap. Two entry points into the same A* core: the `EventBus.OnPathRequested` handler (`CalculatePath`) fires `RaisePathGenerated` with the result and is what `PlayerController` uses; `RequestPathSync(start, end)` returns the path directly instead of broadcasting it, for callers that must not go through the shared event pair (see NPC Patrol below — every subscriber receives every `OnPathGenerated`, so a second broadcaster would make the player walk an NPC's path or vice versa). `GetRandomWalkableCoordinates(Vector2Int from)` does a BFS flood-fill from `from` over walkable neighbors and returns a random cell from the reachable set, so callers never get handed a walkable "island" cell that's cut off by unwalkable tiles (which would otherwise burn a full failed A* search and log a warning). Exposes `SetWalkable(Vector3 worldPos, bool walkable)` to toggle grid cells at runtime (e.g. doors, mission triggers).
- **`InputManager`** — converts `OnMapClicked` world positions to grid coords; if an `IInteractable` is adjacent it calls `Interact()`, otherwise fires `RaisePathRequested`.
- **`PlayerController`** — listens to `OnPathGenerated`, walks the path via a coroutine, flips the `SpriteRenderer` on horizontal movement. Before each step checks the next cell via `Physics2D.OverlapPoint` against an `npcLayerMask`; if blocked, it `yield return null`s once before re-requesting the path from the current position, so the player reroutes around moving entities. That single-frame wait is load-bearing, not cosmetic: re-requesting synchronously can recurse into `StartCoroutine(FollowPath(...))` again within the same call stack, and if the newly computed path is blocked at its own first step too (e.g. a patrol NPC parked on the only route), it recurses without ever yielding and overflows the native stack.

### Mission 2: Clogged River
The interactable that starts the mission is `RiverInteractable` on the waste blockage (not an NPC). Both solutions run inside `ExplorationState` — no new game states needed. Two additional `ContextInteractable` points sit nearby (river running dry, villagers complaining) purely to give the player narrative context before they attempt the 5 Whys quiz — they show dialogue and return straight to `Exploration`; they don't reference a `MissionData` or touch mission state at all.

**Trivial — Pickup Waste:** `MinigameActivator` activates `TrivialContainer`, which holds `WastePickupSystem` and a set of `WastePiece` IInteractables overlapping the blockage visuals. Each `WastePiece.Interact()` hides its paired `wasteVisual` and calls `WastePickupSystem.OnWasteRemoved()`. When remaining count hits zero, fires `RaiseMissionCompleted(2, false)`.

**Optimal — Build Auto Collector:** `MinigameActivator` activates `OptimalContainer`, which holds `PartCollectionSystem` and 3 `MachinePart` IInteractables placed at fixed positions in the editor. Each `MachinePart.Interact()` collects itself and calls `PartCollectionSystem.OnPartCollected()`. At 3/3, `PartCollectionSystem` activates `AssemblyPoint` near the river bank. `AssemblyPoint.Interact()` shows the machine visual and activates `PlacementPoint` at the river bank. `PlacementPoint.Interact()` shows the placed machine visual and fires `RaiseMissionCompleted(2, true)`.

**River reveal:** `RiverManager` listens to `OnMissionCompleted` for missionID 2. On either solution: disables `blockageVisual`, enables `animatedRiverTilemap`.

### Mission 1: Well & Pipe Puzzle
**Trivial — Patch the Well:** `WellVisual` is a plain `IInteractable` (like `RiverInteractable`/`WastePiece`) — its `Interact()` runs a short coroutine and raises `RaiseMissionCompleted(id, false)`. It runs entirely inside `ExplorationState`, no dedicated state needed — same pattern as Mission 2. (There used to be a separate `PatchWellState` gating this via a bespoke `RaiseWellClicked` event; it was removed because that state never allowed player movement, so if the mission-triggering NPC — see NPC Patrol below — had wandered away from the well before dialogue started, the player could get stranded unable to reach it. Since `WellVisual` doesn't need adjacency-free clicking the way the pipe puzzle does, folding it into normal `Exploration` + `InputManager`'s walk-then-interact routing fixed that for free.)

**Optimal — Pipe Puzzle:** `PipeDirection` is a `[Flags]` bitmask enum (Up=1, Right=2, Down=4, Left=8). `PipeNode` holds the current connection bitmask and rotates clockwise via a left bit-shift with wrap-around (`(bits << 1 | bits >> 3) & 15`). `PipeVisual` (MonoBehaviour) reads its `PipeShape` and inspector transform rotation to compute starting bits, then delegates clicks to `PipePuzzleSystem.RotatePipeAt` via the dedicated `Puzzle` state/`RaisePuzzleClicked` event (no adjacency requirement — a precise click on a pipe tile rotates it regardless of player position). The puzzle system runs a DFS flood-fill from `startPos` to `endPos` to check for a valid water path after every rotation, and raises `RaiseMissionCompleted(id, true)` once solved.

### Singletons
`GameManager`, `DialogueManager`, `PlanningUI`, `MissionBoardUI`, `ReflectionPopupUI`, `DayCompleteUI`, `TownSatisfactionSystem`, `InfoBoardUI` all follow the same pattern: static `Instance`, destroyed if a duplicate exists in `Awake`.

### IInteractable
`NPCController`, `MissionBoardInteractable`, `RiverInteractable`, `WastePiece`, `MachinePart`, `AssemblyPoint`, `PlacementPoint`, `TrashPiece`, `TownHallInteractable`, `ContextInteractable`, `WellVisual`, and `InfoBoardInteractable` all implement `IInteractable`. `InputManager` detects them via `Physics2D.OverlapPoint` and calls `Interact()` when the player is within 1 grid cell (or routes the player adjacent first). `ContextInteractable` is the odd one out: it's narrative-only (dialogue with no associated `MissionData`), so `DialogueManager` returns straight to `Exploration` afterward instead of opening `PlanningUI` — it never starts or resolves a mission.

### Info Board
A walk-up-and-interact help/tutorial panel, architecturally a clone of the Mission Board: `InfoBoardInteractable` (`IInteractable`) shows `InfoBoardUI` and changes state to `InfoBoard`; `InfoBoardState` is ESC-only, same shape as `MissionBoardState`. `InfoBoardUI` isn't dialogue-typed — it's a static paged reference (`InfoPage[] pages`, each a `title`/`body`), navigated with Next/Previous buttons wired directly to `ShowNextPage()`/`ShowPreviousPage()` in the Inspector, covering movement, the 5 Whys mechanic, satisfaction, trash, town hall, and a catalog of interactable types. The default page content is a C# field initializer on `InfoBoardUI.pages`, not scene-authored data.

### Minimap
`MinimapCamera` (on a dedicated second `Camera` in the scene) follows the `Player`-tagged object every `LateUpdate`, using the same tag-lookup pattern as `InteractionIndicator`. That camera renders to a RenderTexture displayed by a `RawImage` anchored top-right on the main Canvas — it's a live zoomed-out view of the same scene, not a separate icon-based map.

`InteractionIndicator` is an optional companion component placed on an interactable's GameObject: it shows a bobbing prompt icon whenever the player is within `showRange` during `Exploration`. Call its `Hide()` method once the owning interactable has been permanently consumed (e.g. a collected `MachinePart`, or an `NPCController` whose mission was just resolved — see Mission Board below).

### NPC Patrol
`NPCPatrol` is a standalone component (added alongside `NPCController` on the same NPC GameObject) that wanders an NPC between random walkable tiles. Its coroutine `yield return null`s once before its first loop iteration, because `OnEnable` isn't guaranteed to run after every other object's `Awake` — only that object's own `Awake` is guaranteed before its own `OnEnable`, so touching `GameManager.Instance` synchronously in `OnEnable` can NRE if `GameManager`'s `Awake` hasn't run yet. Each loop iteration: gate on `GameManager.Instance.StateManager.CurrentStateType == GameStateType.Exploration` (idles otherwise, so NPCs freeze mid-step the instant dialogue/a minigame opens rather than sliding around during it), pick a destination via `PathfindingSystem.GetRandomWalkableCoordinates`, path to it via `RequestPathSync`, then step along it with the same move/animate/flip pattern as `PlayerController.FollowPath`. Disabling the NPC's GameObject (e.g. `SetActive(false)`) stops this coroutine for free.

NPCs that should block the player's path (and be avoided by the reroute-on-block logic in `PlayerController`) need a `Collider2D` on a dedicated `NPC` Unity layer, with `PlayerController.npcLayerMask` including that layer. The collider should be a trigger — the avoidance is handled by path-rerouting, not physics collision response.

### Town Satisfaction System
`TownSatisfactionSystem` (singleton, on `ProgressManager` GameObject) owns a single `CurrentSatisfaction` value (0..`MaxSatisfaction`, starts at `startingSatisfaction`). It listens to `OnMissionCompleted`, looks up the completed mission via its `MissionRegistry` reference, and applies `MissionData.optimalSatisfactionReward` or `.trivialSatisfactionReward` (defaults 25 / 10) through its public `ApplyDelta(int)` method, clamping and raising `RaiseSatisfactionChanged(CurrentSatisfaction)` on every change. `SatisfactionBarUI` (always-visible HUD element, no `CanvasGroup` show/hide) subscribes to `OnSatisfactionChanged` and drives a `Filled`-type `Image.fillAmount`.

`TrashSpawner` periodically instantiates a `trashPrefab` at a random unoccupied point from its `spawnPoints` array, and applies a single fixed `-satisfactionPenaltyPerTrash` hit via `TownSatisfactionSystem.ApplyDelta` at the moment each piece spawns — there is no ongoing decay while a piece sits uncleaned. The spawn timer lives in `Update()`, gated by `GameManager.Instance.StateManager.CurrentStateType != GameStateType.Exploration` (early return), so spawning pauses during any non-Exploration state (dialogue, minigames, mission board, day-complete, etc.) and resumes only in `Exploration`. Each `TrashPiece` tracks its own `accumulatedLoss` — `ApplyDelta`'s actual (post-clamp) return value from that one spawn-time hit — and refunds that exact amount on `Interact()` before removing itself from the spawner's occupied set and destroying its GameObject, so cleaning up a piece fully undoes its impact on the bar without over-refunding at the 0/max clamp edges.

### Day Progression & Town Hall Upgrade
Day-end is player-controlled, not automatic. `TownHallInteractable` (on the `TownHall` GameObject, alongside `TownHallUpgrade`) fires `RaiseDayCompleted(currentDay)` directly from `Interact()` whenever the player walks up and interacts with Town Hall — there is no mission-completion gating on this trigger.

`DayCompleteUI` reads `TownSatisfactionSystem.Instance.CurrentSatisfaction` in its `OnDayCompleted` handler and picks a tiered subtitle (`>= 80` thriving / `50-79` mixed progress / `< 50` struggling) — the satisfaction bar is now the actual measure of how the day went, not mission optimality tracking.

`TownHallUpgrade` (on the town hall entity) listens to `OnDayCompleted(int day)` and activates the matching index in its `stages` array, deactivating all others. Index 0 = default, index 1 = Day 1 upgrade, index 2 = Day 2 upgrade. The town hall is built as a multi-child SpriteRenderer GameObject (not tilemaps) so each stage can have a Base sprite (EntityTilemap sorting layer) and a Roof sprite (ForeGroundTilemap sorting layer) to preserve player depth layering.

`EventBus.OnDayCompleted` (`Action<int>`) is the hook for any other system that needs to respond to day advancement.
