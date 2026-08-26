# Kaizen Village — Game Design Document

*Unity 6, 2D top-down. Compiled from the current build (`Assets/Scripts`, `Assets/Data`, `Assets/Scenes/SampleScene.unity`) as of this writing.*

---

## 1. High Concept

Kaizen Village is a 2D top-down educational game that teaches **Kaizen / PDCA (Plan–Do–Check–Act)** continuous-improvement thinking through village-management vignettes. The player walks around a small farming village, listens to villagers describe a problem, interrogates the problem with a guided **5 Whys** investigation, and is routed — based on how well they diagnosed the root cause, not by free choice — into either a quick patch or a proper structural fix. A minigame executes whichever solution was earned, and a reflection message makes the Kaizen lesson explicit: band-aid fixes don't stick.

The core rhetorical trick of the design: **the player never picks trivial vs. optimal directly.** They pick answers to "why" questions. The system converts diagnostic accuracy into the systemic-vs-superficial outcome. Only a perfect diagnosis (5/5) earns the real fix. This mirrors the pedagogical point — genuine root-cause analysis is hard and mostly-right isn't good enough — better than a menu ever could.

## 2. Pillars

1. **Diagnose, don't choose.** The 5 Whys quiz *is* the decision point. There is no separate "pick a solution" UI.
2. **Consequences persist, but aren't punitive dead ends.** A trivial fix isn't a fail state — it's a deferred one. The Stage Gate System (§6) resurfaces it for a redo instead of blocking progress silently or permanently penalizing the player.
3. **Everything is diegetic where possible.** Satisfaction is a bar tied to the village, not an abstract score; trash is a physical, clickable nuisance; upgrades are visible town-hall sprites, not a menu screen.
4. **One state, one responsibility.** Every mode of play (exploring, talking, planning, puzzling, reading a reflection) is an explicit state so input routing never has to guess what the player is currently doing.
5. **No dead-ends from imperfect play.** A wrong 5-Whys answer doesn't block progress mid-quiz (it always advances) and a wrong overall outcome doesn't block the day (it just withholds full credit and reopens later).

## 3. Player Fantasy & Loop

The player is an unnamed problem-solver dropped into a village where infrastructure keeps failing in ways that look simple on the surface but aren't. The moment-to-moment loop:

```
Explore village → find NPC/problem site → Dialogue → 5 Whys quiz (Planning)
   → system scores diagnosis → routes to Trivial or Optimal minigame
   → minigame resolves → Reflection popup states the Kaizen lesson
   → Mission Board updates → repeat for remaining missions in the stage
   → visit Town Hall → gate checks (see §6) → Day Complete / send player back to fix trivial work
```

A session-level loop wraps this: **Stage → Day → next Stage**, with the town hall's sprite literally upgrading as stages clear, so architectural progress is the visible reward layer on top of the satisfaction number.

## 4. Core Systems

### 4.1 State Machine
`GameManager` owns a `GameStateManager` holding one `IState` per `GameStateType`. States never talk to each other directly — only through `GameManager.Instance.StateManager.ChangeState()` or the event bus. This keeps input routing unambiguous: whatever `Tick()` is currently active owns the click.

| State | Responsibility |
|---|---|
| `Exploration` | Free roam; click-to-move / click-to-interact via `InputManager` |
| `Dialogue` | Advances NPC/complaint dialogue lines on click |
| `Planning` | 5 Whys quiz UI; ESC returns to Exploration |
| `Puzzle` | Pipe-puzzle click routing (position-based, not adjacency-based) |
| `Reflection` | Dismiss the post-mission feedback popup |
| `MissionBoard` | Read-only board overlay; ESC to close |
| `DayComplete` | Stage-clear / needs-review summary panel |
| `InfoBoard` | In-game tutorial/reference pages; ESC to close |

### 4.2 Event Bus
A static `EventBus` class of C# events is the *only* coupling mechanism between systems — no domain references another domain's concrete type. Key events: `OnMapClicked → OnPathRequested → OnPathGenerated`, `OnSolutionSelected`, `OnMissionCompleted`, `OnMissionsNeedReview`, `OnDayCompleted`, `OnSatisfactionChanged`. This is what lets, e.g., the river visuals, the satisfaction bar, and the mission board all react to a single `OnMissionCompleted` firing without knowing about each other.

### 4.3 Pathfinding & Movement
A* over a `GridSystem` built from a collision `Tilemap`, using a binary min-heap for the open set. `PlayerController` walks paths via coroutine and reroutes around moving NPCs by waiting exactly one frame before recomputing — long enough to avoid a same-frame recursive stack overflow if the NPC is still blocking the new path's first step. NPCs that should physically block a route sit on a dedicated `NPC` trigger layer so they're avoided without ragdoll-style physics response.

`GetRandomWalkableCoordinates` BFS-flood-fills from a start point so patrol/wander targets are always drawn from the *reachable* set — no failed A* search against an unreachable island tile.

### 4.4 Data Layer
All mission and stage content is authored as ScriptableObjects, not hardcoded — a content designer can add a Mission 3 without touching a state machine or minigame script:

- **`MissionData`** — complaint text, root cause, the 5-Whys chain (`WhyStage[5]`: question/correctAnswer/distractors/hint), both solution names, both reflection texts, and per-outcome satisfaction rewards (default trivial +10 / optimal +25).
- **`MissionRegistry`** — flat array of `MissionData`, looked up by `missionID`.
- **`StageData`** — a stage number/name and the `missionIDs[]` that must all resolve optimally before the stage can close.
- **`StageRegistry`** — array of `StageData`, indexed sequentially.

## 5. The 5 Whys Mechanic (the game's signature system)

Run by `PlanningUI` after it types out both solution names for flavor. Five sequential "Why" stages, each a multiple-choice pick among the correct answer and its distractors.

**Rules:**
- Picking *any* option always advances — there's no hard-blocking retry loop mid-quiz. This keeps pacing snappy; punishment is deferred to the outcome, not friction injected into every question.
- `PlanningUI` tallies `correctCount` across all 5 stages.
- `outcomeIsOptimal = (correctCount == 5)`. Anything less routes to the trivial fix. This is deliberately unforgiving — one slip anywhere in the chain denies full credit, which is the game's thesis: surface-level root-causing isn't root-causing.
- That boolean is what feeds `RaiseSolutionSelected(missionID, outcomeIsOptimal)`, which `MinigameActivator` uses to activate the correct minigame container and switch state.

**Redo behavior differs from a first attempt** (driven by the Stage Gate System, §6):
- `hintText` is suppressed on a first attempt and only shown once `StageManager.IsMissionUnderReview(missionID)` is true — so a first pass is a genuine cold diagnosis, and a forced redo gets scaffolding rather than repeating the same blind guess.
- Each stage's distractor pool excludes whatever the player specifically picked wrong on a prior attempt at that exact stage (`RecordWrongAnswer` / `GetExcludedDistractors`), with a floor guard so a question can never degrade to "only the correct answer is shown." This makes a redo strictly about correcting the specific misunderstanding that failed last time, not re-rolling the same trap.

**Content example — Mission 1 (`M1_ParchedCrops`):** the chain walks from "not enough water reaching crops" → "well isn't drawing enough water" → "water leaking out of the well" → "cracked stone lining" → "old and never reinforced" → root cause: *no proper pulley/filter system to reduce strain on the aging structure.* The distractor set at every stage is designed to tempt shallow-but-plausible answers (rain, pests, tools) that a careless player would pick if they weren't tracing the causal chain the complaint text actually implies.

## 6. Mission Flow (canonical happy path)

1. Player clicks an `IInteractable` NPC → `DialogueState`.
2. `DialogueManager` exhausts complaint lines → opens `PlanningUI` → `PlanningState`.
3. `PlanningUI` types the trivial solution name, then the optimal one, then runs the 5 Whys quiz.
4. On the 5th answer, `PlanningUI` computes the outcome and fires `RaiseSolutionSelected`. `MinigameActivator` activates the matching container (trivial or optimal) and switches to that container's inspector-assigned target state (`Exploration` for click-driven minigames, `Puzzle` for the pipe grid).
5. The minigame resolves → `RaiseMissionCompleted(missionID, wasOptimal)`.
6. `ReflectionPopupUI` shows the matching reflection text from `MissionData`, switches to `Reflection`.
7. Click to dismiss → back to `Exploration`. `MissionBoardUI` greys out that entry.

A trivial resolution is *not* final — see §7.

## 7. Stage Gate System (the retention/replay layer)

This is the mechanism that keeps a "good enough" trivial fix from quietly counting as done — without ever hard-blocking the player from continuing to explore and act on other missions.

`StageManager` groups missions by `StageData` and tracks each mission's most recent outcome. **Only a fully-optimal stage submits.**

**Town Hall gates `Interact()` through three checks, in order, before allowing submission:**
1. `AllStagesComplete` → shows a closing dialogue, stops (game is content-complete).
2. `AllMissionsCompleteForCurrentStage()` → if any mission hasn't been touched at all yet, shows an "outstanding problems" dialogue.
3. `TrashSpawner.HasLiveTrash` → if any trash piece is on the ground, shows a "clear the streets first" dialogue.

Only once all three pass does `SubmitStage()` run.

**`SubmitStage()` branches on the stage's mission outcomes:**
- **All optimal** → advances `currentDay`, fires `OnDayCompleted`, resets satisfaction to baseline directly (bypassing the event bus so `DayCompleteUI` can't race-read the score before the reset), clears per-stage tracking state, advances to the next `StageData` (or flips `AllStagesComplete`).
- **Any still trivial** → fires `OnMissionsNeedReview(missionIDs[])` *first* (so the day-complete panel shows the satisfaction the player actually earned this attempt, before anything is clawed back), then retracts each flagged mission's trivial reward via `RetractTrivialReward` — gated by a `pendingRetraction` flag so a still-wrong redo can't double-dip credit.

**`OnMissionsNeedReview` puts the flagged mission back to its pre-completion state in place** — no scene reload, no re-walking to a checkpoint:
- `NPCController` clears its completed flag and re-shows its interaction indicator.
- `RiverInteractable` re-activates itself.
- `PipePuzzleSystem` resets every pipe to its *cached original* rotation/bitmask.
- `PartCollectionSystem` / `WastePickupSystem` reset collected counts and re-show pieces.
- `MissionEntryUI` un-greys.

Components that live inside a `MinigameActivator` container that gets *disabled* on completion subscribe to `OnMissionsNeedReview` in `Awake`/`OnDestroy` rather than `OnEnable`/`OnDisable` — an `OnEnable` subscription would already be torn down by the time a review request (which can only fire after completion) needs to reach a disabled object.

The redo then runs through the *same* 5 Whys quiz, with the hint/distractor-exclusion scaffolding from §5 active. This is the design's actual "Check → Act" loop made mechanical: fail the check, get a scaffolded second attempt, re-submit.

## 8. Town Satisfaction System

A single visible resource that stands in for "is Kaizen actually working here." `TownSatisfactionSystem` (singleton) starts at **50 / 100**.

- **Missions add to it.** `+25` for an optimal resolution, `+10` for trivial (both are per-`MissionData`, tunable per mission) — applied once, on `OnMissionCompleted`.
- **Trash subtracts from it.** `TrashSpawner` periodically spawns a piece at a random unoccupied point and applies a flat **-5** the instant it spawns (not ongoing decay). Spawning is paused outside `Exploration`, so nothing punishes the player for being mid-dialogue or mid-minigame.
- **Cleaning up trash refunds exactly what that piece cost.** Each `TrashPiece` remembers its own actual post-clamp delta and reverses precisely that amount on pickup — so the bar can't be gamed by farming spawn/clamp edges, and can't over-refund.
- **Only a full stage pass resets it to baseline.** A mission being sent back for review does *not* touch satisfaction or trash — those are consequences of *day advancement*, not of catching a bad diagnosis.
- Drives a single always-visible `SatisfactionBarUI` fill bar; also drives `DayCompleteUI`'s tiered subtitle on a real stage pass (≥80 thriving / 50–79 mixed progress / <50 struggling) — satisfaction, not mission-optimality count, is the stated read on "how the day went."

## 9. Missions

### Mission 1 — "The Parched Crops" (`missionID: 1`)
**Complaint:** *"There's not enough water to water the crops!"* — raised by the `Farmer_NPC`.
**Root cause:** the central well's stone lining is cracked from age with no reinforcement, leaking water into the soil before it reaches the surface.

| | Trivial | Optimal |
|---|---|---|
| Name | *Patch the bucket and rope* | *Rebuild the pulley and pipe filter system* |
| Mechanic | `WellVisual` — a single `IInteractable` (`Container_Trivial_M1`) that plays a short coroutine and immediately completes | `Container_Optimal_M1` — a 3×3 rotate-the-pipes puzzle (`PipePuzzleSystem`) |
| Reflection | *"The patch worked for now, but the root cause remains. The well will fail again soon..."* | *"Great work! Rebuilding the pulley system fixed the root cause. Water flows properly now."* |

**Pipe puzzle mechanics:** `PipeDirection` is a `[Flags]` bitmask (Up/Right/Down/Left). Each `PipeVisual` reads its `PipeShape` + authored rotation to compute a starting bitmask; clicking rotates it clockwise via `(bits << 1 | bits >> 3) & 15`. Clicks route through the dedicated `Puzzle` state and `RaisePuzzleClicked` — **position-based, not adjacency-gated**, since the puzzle is a fixed board the player looks at rather than a thing they walk up to piece-by-piece. After every rotation, a DFS flood-fill checks for a connected water path from `startPos` (0,0) to `endPos` (2,2); a valid path fires `RaiseMissionCompleted(1, true)`. Original per-tile bitmask/rotation is cached at scene start (including from *inactive* pipes, since this container may never activate if the player earns the trivial path instead) so a Stage-Gate reset can restore the exact starting board.

**Design note:** the well used to have a bespoke `PatchWellState` that blocked player movement entirely. It was removed because the triggering NPC can wander (via `NPCPatrol`) away from the well before the player finishes dialogue, which could strand the player in a state where nothing was reachable. Folding the trivial path into ordinary `Exploration` + `InputManager`'s walk-then-interact routing fixed that for free, since a click on `WellVisual` doesn't need the puzzle's no-adjacency click model anyway.

### Mission 2 — "Clogged River" (`missionID: 2`)
**Complaint:** *"It's hard to catch any fish nowadays!"*
**Root cause:** rubbish has piled up upstream with no collection system, starving the downstream flow (and the fish with it).

Unlike Mission 1, the trigger is **not** an NPC — it's `RiverInteractable` sitting directly on the waste blockage in the world. Two additional `ContextInteractable` points nearby (a dried riverbed, complaining villagers) are pure narrative flavor: they show dialogue and return straight to Exploration without touching any `MissionData` or mission state, giving the player context before they ever open the 5 Whys quiz.

| | Trivial | Optimal |
|---|---|---|
| Name | *Clear the rubbish* | *Set up an automatic rubbish collector* |
| Mechanic | `WastePickupSystem` + N `WastePiece` interactables overlapping the blockage art; each click hides its visual and decrements a counter | `PartCollectionSystem` + 3 fixed `MachinePart` pickups → `AssemblyPoint` (assemble) → `PlacementPoint` at the riverbank (install) |
| Reflection | *"The rubbish has been cleared, but the root cause hasn't been solved..."* | *"Good! You demonstrated Jidoka in this mission choice by creating the automatic rubbish cleaner and solving the root cause."* |

Both paths ultimately fire `RaiseMissionCompleted(2, wasOptimal)`, which `RiverManager` listens for regardless of which path was taken: it disables the `blockageVisual` and enables `animatedRiverTilemap` either way — the *visual* payoff (river flowing again) is identical, deliberately, so the game doesn't spoil "this was the wrong fix" before the reflection text says so.

**Optimal path is the game's clearest Jidoka example** (a machine that keeps the fix running automatically instead of a human repeating the same manual chore) — the reflection text names the Lean principle explicitly, tying the fiction back to the pedagogy.

## 10. Ancillary Systems

### Mission Board
One `MissionEntryUI` per mission, Inspector-assigned. On completion, the entry greys (`alpha 0.4`) and reads **"Resolved"** (optimal) or **"Needs Review"** (trivial). `NPCController.HandleSolutionSelected` sets a `missionCompleted` no-op flag and hides the NPC's `InteractionIndicator` on selection — but the NPC GameObject stays active and, if it has `NPCPatrol`, keeps wandering. A "Needs Review" entry can *only* be reopened via the Stage Gate System rejecting a stage submission — there's no manual "redo mission" button, which keeps the Check/Act step tied to the Town Hall checkpoint rather than something the player can trivially spam.

### Info Board
A walk-up tutorial panel (architectural clone of the Mission Board: `InfoBoardInteractable` → `InfoBoardUI` + `InfoBoardState`, ESC-only). A static, paged reference (`InfoPage[]`, Next/Previous buttons) covering: Welcome, Getting Around, Talking to Villagers, The 5 Whys, Missions & the Mission Board, Town Satisfaction, Trash, Town Hall & New Days, What You'll Find Around Town. Exists so the game can explain its own mechanics diegetically instead of a forced onboarding sequence.

### Day Progression & Town Hall Upgrade
`TownHallUpgrade` listens for `OnDayCompleted(day)` and swaps the active sprite set by index (0 = default, 1 = Day-1 upgrade, 2 = Day-2 upgrade). The town hall is built from separate Base/Roof sprites on `EntityTilemap`/`ForeGroundTilemap` sorting layers specifically so the roof can still render in front of the player while the base renders behind — i.e. stage progress is legible from across the map without breaking depth sorting.

### Minimap
A second camera (`MinimapCamera`) tracks the `Player` tag every `LateUpdate` and renders to a `RawImage` pinned top-right — a live, zoomed-out view of the same scene rather than an icon-based abstraction, consistent with the game's preference for diegetic feedback over HUD abstraction.

### NPC Patrol
`NPCPatrol` wanders NPCs between random walkable tiles, but only while the global state is `Exploration` — NPCs freeze mid-step the instant dialogue or a minigame opens, so nothing looks like it's sliding around behind a modal panel. Patrol targets come from the same BFS-reachability-checked `GetRandomWalkableCoordinates` the pathfinding system exposes, so an NPC never picks a target isolated by unwalkable tiles.

## 11. Content Inventory (current scene)

- **Stage 1** (`Stage1.asset`): missions `[1, 2]` — both must resolve optimally to submit.
- **Missions authored:** `M1_ParchedCrops` (well/farm), `M2_CleaningRiver` (river/waste) — both have complete 5-Whys chains and both solution paths implemented and wired in-scene.
- **Notable scene objects:** `Farmer_NPC` (Mission 1 trigger, patrols), `RiverBlockagePoint`/`VIllagerComplaintPoint`/`RiverDryPoint` (Mission 2 trigger + 2 context points), `Container_Trivial_M1`/`Container_Optimal_M1`, `Container_Trivial_M2`/`Container_Optimal_M2`, `TownHall` (with `blackSmithBase_1`/`blackSmithRoof_1`-style stage sprites), `ProgressManager` (satisfaction + trash), `MissionBoard`, `InfoBoard`, `MinimapCamera`.
- **Tuned values:** starting satisfaction 50/100; trash penalty -5/spawn; trash spawn interval randomized 25–45s, paused outside Exploration; mission rewards default +10 trivial / +25 optimal (both overridable per-`MissionData`).

## 12. Design Rationale Notes (why it's built this way)

- **Quiz-drives-outcome instead of a solution picker** removes the "just pick optimal, it sounds better" meta-strategy a menu invites — the player has to actually reason through causality to earn it, which is the whole point of teaching 5 Whys.
- **All-5-or-trivial (no partial credit tiers)** was a deliberate design choice, not a missed nuance — the note in `PlanningUI`'s design ("hitting all 5 is intentionally hard") signals the team wants root-causing to feel genuinely hard to nail, not a coin-flip.
- **Rejection reopens in place rather than restarting the mission** keeps the loop's cost proportional to the mistake — the player doesn't replay dialogue or re-walk across the map, only re-answers the quiz (now scaffolded) and, for Mission 1, re-solves a puzzle that's been reset to its original layout.
- **Satisfaction only resets on a full stage pass, not on a review flag** — this means limping a stage through with a mixed record and then failing to fix it before Town Hall doesn't erase the satisfaction the player *did* earn from other missions; only forward progress (a full pass) is what "banks" a fresh baseline.
- **Event bus as the sole coupling layer** is what makes the Stage Gate reset (§7) tractable at all: `OnMissionsNeedReview` reaches five-plus unrelated systems (NPC, river, puzzle, part/waste collection, mission board) without any of them referencing each other or the `StageManager` directly.
