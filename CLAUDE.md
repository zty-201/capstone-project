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
| `PatchWell` | Polls mouse clicks, fires `RaiseWellClicked` |
| `Reflection` | Delegates left-click to `ReflectionPopupUI.OnDismiss()` |
| `MissionBoard` | ESC returns to Exploration |

### Event Bus
`EventBus` is a static class of C# events. Systems subscribe in `OnEnable`/`OnDisable` and raise via the `Raise*` helpers. This is the only coupling layer between systems — no direct references across domains.

Key events: `OnMapClicked → OnPathRequested → OnPathGenerated`, `OnSolutionSelected`, `OnMissionCompleted`.

### Mission Flow (complete happy path)
1. Player clicks NPC (`IInteractable.Interact()`) → `DialogueState`
2. `DialogueManager` exhausts all lines → opens `PlanningUI` → `PlanningState`
3. `PlanningUI` type-animates trivial solution name, then optimal solution name, then shows choice buttons
4. Player picks a solution → `RaiseSolutionSelected(missionID, type)` → `MinigameActivator` activates the right container and changes to its inspector-assigned `targetState`
5. Puzzle solved / well patched → `RaiseMissionCompleted(missionID, wasOptimal)`
6. `ReflectionPopupUI` listens, shows feedback text from `MissionData`, changes state to `Reflection`
7. Player clicks to dismiss → `Exploration`; `MissionBoardUI` listens to grey out the entry

### Data Layer (ScriptableObjects)
- **`MissionData`** — all text content for one mission: complaint, root cause, 5W fields, solution names, reflection texts. Create via `Kaizen Systems/Mission Data`.
- **`MissionRegistry`** — array of `MissionData`, looked up by `missionID`. Create via `Kaizen Systems/Mission Registry`. Assign in Inspector on `ReflectionPopupUI`.

### Pathfinding & Grid
- **`GridSystem`** (pure C#) — 2D array of `GridNode`; converts between world positions and grid coordinates.
- **`PathfindingSystem`** (MonoBehaviour) — builds a `GridSystem` in `Start`, reads a collision `Tilemap` to mark unwalkable cells, runs A* using a `NodeMinHeap` min-heap on `EventBus.OnPathRequested`, fires `RaisePathGenerated` with the result. Exposes `SetWalkable(Vector3 worldPos, bool walkable)` to toggle grid cells at runtime (e.g. doors, mission triggers).
- **`InputManager`** — converts `OnMapClicked` world positions to grid coords; if an `IInteractable` is adjacent it calls `Interact()`, otherwise fires `RaisePathRequested`.
- **`PlayerController`** — listens to `OnPathGenerated`, walks the path via a coroutine, flips the `SpriteRenderer` on horizontal movement. Before each step checks the next cell via `Physics2D.OverlapPoint` against an `npcLayerMask`; if blocked, re-requests the path from the current position so the player reroutes around moving entities.

### Mission 2: Clogged River
The interactable is `RiverInteractable` on the waste blockage (not an NPC). Both solutions run inside `ExplorationState` — no new game states needed.

**Trivial — Pickup Waste:** `MinigameActivator` activates `TrivialContainer`, which holds `WastePickupSystem` and a set of `WastePiece` IInteractables overlapping the blockage visuals. Each `WastePiece.Interact()` hides its paired `wasteVisual` and calls `WastePickupSystem.OnWasteRemoved()`. When remaining count hits zero, fires `RaiseMissionCompleted(2, false)`.

**Optimal — Build Auto Collector:** `MinigameActivator` activates `OptimalContainer`, which holds `PartCollectionSystem` and 3 `MachinePart` IInteractables placed at fixed positions in the editor. Each `MachinePart.Interact()` collects itself and calls `PartCollectionSystem.OnPartCollected()`. At 3/3, `PartCollectionSystem` activates `AssemblyPoint` near the river bank. `AssemblyPoint.Interact()` shows the machine visual and activates `PlacementPoint` at the river bank. `PlacementPoint.Interact()` shows the placed machine visual and fires `RaiseMissionCompleted(2, true)`.

**River reveal:** `RiverManager` listens to `OnMissionCompleted` for missionID 2. On either solution: disables `blockageVisual`, enables `animatedRiverTilemap`.

### Mission 1: Pipe Puzzle
`PipeDirection` is a `[Flags]` bitmask enum (Up=1, Right=2, Down=4, Left=8). `PipeNode` holds the current connection bitmask and rotates clockwise via a left bit-shift with wrap-around (`(bits << 1 | bits >> 3) & 15`). `PipeVisual` (MonoBehaviour) reads its `PipeShape` and inspector transform rotation to compute starting bits, then delegates clicks to `PipePuzzleSystem.RotatePipeAt`. The puzzle system runs a DFS flood-fill from `startPos` to `endPos` to check for a valid water path after every rotation. Trivial solution (`WellVisual`) raises `RaiseMissionCompleted(id, false)`; pipe puzzle raises `(id, true)`.

### Singletons
`GameManager`, `DialogueManager`, `PlanningUI`, `MissionBoardUI`, `ReflectionPopupUI` all follow the same pattern: static `Instance`, destroyed if a duplicate exists in `Awake`.

### IInteractable
`NPCController`, `MissionBoardInteractable`, `RiverInteractable`, `WastePiece`, `MachinePart`, `AssemblyPoint`, and `PlacementPoint` all implement `IInteractable`. `InputManager` detects them via `Physics2D.OverlapPoint` and calls `Interact()` when the player is within 1 grid cell (or routes the player adjacent first).

### Day Progression & Town Hall Upgrade
`DayProgressTracker` (on `ProgressManager` GameObject) listens to `OnMissionCompleted` and tracks optimal completions per day. When all required missions for a day are completed optimally, it fires `RaiseDayCompleted(day)`. Configure per instance via `day` (int) and `requiredOptimalMissions` (int array) in the Inspector — Day 1 uses `{ 1, 2 }`.

`TownHallUpgrade` (on the town hall entity) listens to `OnDayCompleted(int day)` and activates the matching index in its `stages` array, deactivating all others. Index 0 = default, index 1 = Day 1 upgrade, index 2 = Day 2 upgrade. The town hall is built as a multi-child SpriteRenderer GameObject (not tilemaps) so each stage can have a Base sprite (EntityTilemap sorting layer) and a Roof sprite (ForeGroundTilemap sorting layer) to preserve player depth layering.

`EventBus.OnDayCompleted` (`Action<int>`) is the hook for any other system that needs to respond to day advancement.
