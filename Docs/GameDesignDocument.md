# Kaizen Village — Game Design Document

*Unity 6, 2D top-down. Compiled from the current build (`Assets/Scripts`, `Assets/Data`, `Assets/Scenes/SampleScene.unity`) as of this writing.*

---

## 1. High Concept

Kaizen Village is a 2D top-down educational game that teaches **Kaizen / PDCA (Plan–Do–Check–Act)** continuous-improvement thinking through village-management vignettes. The player walks around a small farming village, listens to villagers describe a problem, interrogates the problem with a guided **5 Whys** investigation, and is routed — based on how well they diagnosed the root cause, not by free choice — into either a quick patch or a proper structural fix. A minigame executes whichever solution was earned, and a reflection message makes the Kaizen lesson explicit: band-aid fixes don't stick.

The core rhetorical trick of the design: **the player never picks trivial vs. optimal directly.** They pick answers to "why" questions. The system converts diagnostic accuracy into the systemic-vs-superficial outcome. Only a perfect diagnosis (5/5) earns the real fix. This mirrors the pedagogical point — genuine root-cause analysis is hard and mostly-right isn't good enough — better than a menu ever could.

## 2. Pillars

1. **Diagnose, don't choose.** The 5 Whys quiz *is* the decision point. There is no separate "pick a solution" UI.
2. **Consequences persist, but aren't punitive dead ends.** A trivial fix isn't a fail state — it's a deferred one. The Stage Gate System (§6) resurfaces it for a redo instead of blocking progress silently or permanently penalizing the player.
3. **Everything is diegetic where possible.** Progress is a real inventory of Gold Coins the player earns and physically carries to Town Hall, not an abstract score; trash is a physical nuisance that has to be picked up and carried to a collection site, not a number that just decays; upgrades are visible town-hall sprites, not a menu screen.
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

A session-level loop wraps this: **Stage → Day → next Stage**, with the town hall's sprite literally upgrading as stages clear, so architectural progress is the visible reward layer on top of the Gold Coin economy (§8).

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
A static `EventBus` class of C# events is the *only* coupling mechanism between systems — no domain references another domain's concrete type. Key events: `OnMapClicked → OnPathRequested → OnPathGenerated`, `OnSolutionSelected`, `OnMissionCompleted`, `OnMissionsNeedReview`, `OnDayCompleted`, `OnInventoryChanged`, `OnTrustChanged`, `OnPDCAPhaseChanged`. This is what lets, e.g., the river visuals, the inventory HUD, the trust pips, and the mission board all react to a single `OnMissionCompleted` firing without knowing about each other.

### 4.3 Pathfinding & Movement
A* over a `GridSystem` built from a collision `Tilemap`, using a binary min-heap for the open set. `PlayerController` walks paths via coroutine and reroutes around moving NPCs by waiting exactly one frame before recomputing — long enough to avoid a same-frame recursive stack overflow if the NPC is still blocking the new path's first step. NPCs that should physically block a route sit on a dedicated `NPC` trigger layer so they're avoided without ragdoll-style physics response.

`GetRandomWalkableCoordinates` BFS-flood-fills from a start point so patrol/wander targets are always drawn from the *reachable* set — no failed A* search against an unreachable island tile.

### 4.4 Data Layer
All mission and stage content is authored as ScriptableObjects, not hardcoded — a content designer can add a Mission 3 without touching a state machine or minigame script:

- **`MissionData`** — complaint text, root cause, the 5-Whys chain (`WhyStage[5]`: question/correctAnswer/distractors/hint), both solution names, both reflection texts. No longer carries any reward numbers — see §8, Gold Coins are a flat, mission-agnostic reward now.
- **`MissionRegistry`** — flat array of `MissionData`, looked up by `missionID`.
- **`StageData`** — a stage number/name and the `missionIDs[]` that must all resolve optimally before the stage can close.
- **`StageRegistry`** — array of `StageData`, indexed sequentially.
- **`ItemData`** — one inventory item's identity (`itemID`/`itemName`/`icon`) and stacking rules (`stackable`/`maxStack`). Two assets exist: **Gold Coin** (stackable) and **Trash** (not stackable, so litter piles up one slot per piece instead of quietly stacking away).

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

**Town Hall gates `Interact()` through four checks, in order, before allowing submission:**
1. `AllStagesComplete` → shows a closing dialogue, stops (game is content-complete).
2. `AllMissionsCompleteForCurrentStage()` → if any mission hasn't been touched at all yet, shows an "outstanding problems" dialogue.
3. `TrashSpawner.HasLiveTrash` → if any trash piece is on the ground, shows a "clear the streets first" dialogue.
4. `AllMissionsOptimalForCurrentStage() && !HasEnoughCoins()` → every mission is solved optimally but the player isn't carrying enough Gold Coins yet, shows a "bring two gold coins" dialogue.

Only once all four pass does `SubmitStage()` run.

**`SubmitStage()` branches on the stage's mission outcomes:**
- **All optimal** → consumes `coinsRequiredToSubmit` (2) Gold Coins from `InventorySystem` (guaranteed to succeed — Town Hall already confirmed there were enough before calling in), advances `currentDay`, fires `OnDayCompleted`, clears per-stage tracking state, advances to the next `StageData` (or flips `AllStagesComplete`).
- **Any still trivial** → fires `OnMissionsNeedReview(missionIDs[])`. There's nothing to claw back on a failed redo: trivial completions never earned a Gold Coin in the first place (see §8), so the coin count already reflects exactly what the player has actually earned — no separate retraction step needed, unlike the old satisfaction system this replaced.

**`OnMissionsNeedReview` puts the flagged mission back to its pre-completion state in place** — no scene reload, no re-walking to a checkpoint:
- `NPCController` clears its completed flag and re-shows its interaction indicator.
- `RiverInteractable` re-activates itself.
- `PipePuzzleSystem` resets every pipe to its *cached original* rotation/bitmask.
- `PartCollectionSystem` / `WastePickupSystem` reset collected counts and re-show pieces.
- `MissionEntryUI` un-greys.

Components that live inside a `MinigameActivator` container that gets *disabled* on completion subscribe to `OnMissionsNeedReview` in `Awake`/`OnDestroy` rather than `OnEnable`/`OnDisable` — an `OnEnable` subscription would already be torn down by the time a review request (which can only fire after completion) needs to reach a disabled object.

The redo then runs through the *same* 5 Whys quiz, with the hint/distractor-exclusion scaffolding from §5 active. This is the design's actual "Check → Act" loop made mechanical: fail the check, get a scaffolded second attempt, re-submit.

## 8. Gold Coin Economy & Inventory

There's no abstract progress meter anymore — `TownSatisfactionSystem`/`SatisfactionBarUI` were
removed outright and replaced with a real inventory the player physically carries, in service of
Pillar 3 (diegetic feedback over HUD abstraction).

**`InventorySystem`** (singleton) owns a fixed array of 8 `InventorySlot` (plain `ItemData item` +
`int count`, not a `MonoBehaviour`). `TryAddItem` stacks into an existing slot when the item is
stackable and there's room, otherwise claims the first empty slot, and returns `false` if nothing
fits. Every mutation fires `OnInventoryChanged` (no payload — subscribers just re-read `Slots`).
`InventoryUI` is a fixed array of slot `Image`/count-text pairs that refresh on that event,
occupying the screen position the old satisfaction bar used to hold.

**Earning coins:** `CoinRewardSystem` listens to `OnMissionCompleted` and awards exactly 1 Gold
Coin — but only when `wasOptimal`. A trivial completion earns nothing, which is deliberate: it
means there's nothing to claw back later if that mission gets flagged for review and reattempted
(see §7) — the coin count is always an honest, un-gameable record of missions actually solved at
the root cause. The Gold Coin `ItemData` is stackable, so every coin the player is carrying lives
in a single slot.

**Spending coins:** Town Hall requires `coinsRequiredToSubmit` (2) Gold Coins on hand — on top of
every mission in the stage being resolved optimally — before a stage submission is allowed to go
through (§7). This is the game's one hard, numeric gate; everything else about "how the day went"
is legible directly from the mission board and the village itself rather than a summarized score.

**Trash** is now something the player physically carries rather than a satisfaction penalty.
`TrashSpawner` periodically spawns a piece at a random unoccupied point — spawning is purely
presence-based now, no numeric penalty on spawn, and it still pauses outside `Exploration` so
nothing punishes the player for being mid-dialogue or mid-minigame. `TrashPiece.Interact()` tries
to add itself to the inventory (the Trash `ItemData` is **not** stackable, so every piece claims
its own slot — letting litter pile up meaningfully crowds out Gold Coins and other items); on
success it's removed from the ground, on failure (inventory full) it's left untouched rather than
lost. **`TrashCollectionSite`** is a plain interactable placed in the village — one interact clears
every Trash slot in the inventory at once. A mission being sent back for review does *not* touch
trash or the player's coins — those are consequences of *day advancement*, not of catching a bad
diagnosis, exactly as satisfaction/trash used to work under the old system.

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

### Mission 2 — "The Stagnant Pond" (`missionID: 2`)
**Complaint:** *"The pond's gone still and green — we wash and draw drinking water from there, and some of us have gotten sick."*
**Root cause:** the cliff face above the falls has been quietly eroding for seasons; a rockslide has jammed a boulder at the lip of the falls, and every past slide has only ever been shoved aside by hand — nothing was ever built to clear one safely and keep the channel clear, so the pond keeps losing its fresh inflow and stagnating.

The map's river now runs from a cliff-top source down a waterfall into the village pond — a
rework of the original "clogged river" fiction to match the renovated map art (cliff → falls →
pond, not a flat riverside blockage). The underlying mechanics are unchanged; only the fiction and
`MissionData` content were re-skinned, per the "match existing structure" convention (see
`CLAUDE.md`) — reuse what already works rather than build a parallel system for what is fictively
a new scenario.

Unlike Mission 1, the trigger is **not** an NPC — it's `RiverInteractable` sitting directly on the
boulder wedged at the lip of the falls (a natural rockslide, not litter or a human cause). Two
additional `ContextInteractable` points nearby (the thinned-out riverbed below the falls,
villagers warning that the pond water is unsafe to drink or wash in) are pure narrative flavor:
they show dialogue and return straight to Exploration without touching any `MissionData` or
mission state, giving the player context before they ever open the 5 Whys quiz.

| | Trivial | Optimal |
|---|---|---|
| Name | *Clear the loose rubble* | *Rig a cliffside winch* |
| Mechanic | `WastePickupSystem` + N `WastePiece` interactables overlapping rubble shaken loose by the slide; each click hides its visual and decrements a counter | `PartCollectionSystem` + 3 fixed `MachinePart` pickups → `AssemblyPoint` (assemble the winch) → `PlacementPoint` at the cliff lip (anchor it) |
| Reflection | *"You've cleared enough loose rock for a trickle to get through — the pond stirs a little, but nowhere near enough to flush out the stagnant water. This will happen again."* | *"With the rig anchored at the lip, you finally lever the wedged stone free. The falls roar back to full flow, flushing the pond clean — and the rig stays bolted in place to catch whatever comes down next."* |

Both paths ultimately fire `RaiseMissionCompleted(2, wasOptimal)`, which `RiverManager` listens for regardless of which path was taken: it disables the `blockageVisual` (the wedged boulder) and enables `animatedRiverTilemap` either way — the *visual* payoff (falls flowing again) is identical, deliberately, so the game doesn't spoil "this was the wrong fix" before the reflection text says so.

**Optimal path is still the game's clearest Jidoka example** (a mechanism that keeps the fix running automatically instead of a human repeating the same manual chore — here, literally standing guard against the *next* rockslide) — the reflection text implies the Lean principle through what the rig actually does, even without naming it outright, tying the fiction back to the pedagogy.

## 10. Ancillary Systems

### Mission Board
One `MissionEntryUI` per mission, Inspector-assigned. On completion, the entry greys (`alpha 0.4`) and reads **"Resolved"** (optimal) or **"Needs Review"** (trivial). `NPCController.HandleSolutionSelected` sets a `missionCompleted` no-op flag and hides the NPC's `InteractionIndicator` on selection — but the NPC GameObject stays active and, if it has `NPCPatrol`, keeps wandering. A "Needs Review" entry can *only* be reopened via the Stage Gate System rejecting a stage submission — there's no manual "redo mission" button, which keeps the Check/Act step tied to the Town Hall checkpoint rather than something the player can trivially spam.

### Info Board
A walk-up tutorial panel (architectural clone of the Mission Board: `InfoBoardInteractable` → `InfoBoardUI` + `InfoBoardState`, ESC-only). A static, paged reference (`InfoPage[]`, Next/Previous buttons) covering: Welcome, Getting Around, Talking to Villagers, The 5 Whys, Missions & the Mission Board, The PDCA Cycle, Gold Coins & Trust, Trash & Your Inventory, Town Hall & New Days, What You'll Find Around Town. Exists so the game can explain its own mechanics diegetically instead of a forced onboarding sequence.

### NPC Trust Meter
`TrustSystem` (singleton) tracks a `0..maxTrust` (default 5, starting at 2) trust value per
`missionID`: `+1` on an optimal resolution, `-1` on a trivial one, clamped, firing
`OnTrustChanged(missionID, newTrust)`. It's intentionally **visual-only** — trust reflects mission
outcome history but doesn't gate anything; reattempting a trivial mission is still handled
entirely by the Stage Gate System (§7). Trust also persists across stages/days, since it's a
standing relationship signal rather than per-stage bookkeeping. `NPCTrustUI` renders it as a row
of pip `SpriteRenderer`s (not UI `Image`s — see below) on the mission-giving object for each
mission, reading the starting value in `Start()` and then listening for updates.

### World-Attached NPC UI
World-attached indicators (the prompt icon, trust pips) default to `SpriteRenderer` + sorting
layer — the same rendering system every other world object in the scene already uses — rather
than a World Space `Canvas`. This is a direct instance of the "match existing structure" design
principle (§12): a Canvas would technically work, but it would mean two parallel answers to "how
do I show something above an object in the world" instead of one. Escalate to a World Space
`Canvas` only when an element needs a genuine UI-only capability `SpriteRenderer` can't do
(`Image.fillAmount`, layout groups, interactive widgets); floating dynamic text uses mesh-based
`TextMeshPro`, not `TextMeshProUGUI`, so text alone isn't a reason to escalate either.

### PDCA Phase Indicator
A HUD element (`PDCAIndicatorUI`) makes the Plan-Do-Check-Act framing visible while playing,
driven by `OnPDCAPhaseChanged(PDCAPhase)` rather than `GameStateType` — most of the "Do" minigames
(well patch, waste pickup, part collection) run inside plain `Exploration` with no dedicated state
of their own, so the indicator can't be state-driven the way the state machine is. `PDCAPhase` is
`{ None, Plan, Do, Check }` — "Act" is deliberately not a distinct visible phase; the indicator
just hides (`None`) on return to Exploration, standing in for "go apply what you learned" without
a dedicated screen to anchor a 4th label to. Four single-line raise points mark the exact
mission-scoped moment each phase begins: `PlanningUI.Show` → `Plan`; `MinigameActivator`
activating its container → `Do`; `ReflectionPopupUI` showing the popup → `Check`;
`ReflectionPopupUI.OnDismiss` → `None`.

### Interact SFX
Every `IInteractable` exposes an `AudioClip InteractSfx` — `InputManager` plays it via
`AudioManager.PlaySFX` immediately after calling `Interact()`, so there's exactly one call site for
interact audio across all 13 interactable types, and each type can carry its own distinct clip (or
none, silently) without duplicating playback logic per script.

### Day Progression & Town Hall Upgrade
`TownHallUpgrade` listens for `OnDayCompleted(day)` and swaps the active sprite set by index (0 = default, 1 = Day-1 upgrade, 2 = Day-2 upgrade). The town hall is built from separate Base/Roof sprites on `EntityTilemap`/`ForeGroundTilemap` sorting layers specifically so the roof can still render in front of the player while the base renders behind — i.e. stage progress is legible from across the map without breaking depth sorting.

### Minimap
A second camera (`MinimapCamera`) tracks the `Player` tag every `LateUpdate` and renders to a `RawImage` pinned top-right — a live, zoomed-out view of the same scene rather than an icon-based abstraction, consistent with the game's preference for diegetic feedback over HUD abstraction.

### NPC Patrol
`NPCPatrol` wanders NPCs between random walkable tiles, but only while the global state is `Exploration` — NPCs freeze mid-step the instant dialogue or a minigame opens, so nothing looks like it's sliding around behind a modal panel. Patrol targets come from the same BFS-reachability-checked `GetRandomWalkableCoordinates` the pathfinding system exposes, so an NPC never picks a target isolated by unwalkable tiles.

## 11. Content Inventory (current scene)

- **Stage 1** (`Stage1.asset`): missions `[1, 2]` — both must resolve optimally to submit.
- **Missions authored:** `M1_ParchedCrops` (well/farm), `M2_CleaningRiver` (asset name predates the "Stagnant Pond" rework in §9 — content and 5-Whys chain updated in place, filename unchanged) — both have complete 5-Whys chains and both solution paths implemented and wired in-scene.
- **Notable scene objects:** `Farmer_NPC` (Mission 1 trigger, patrols), `RiverBlockagePoint`/`VIllagerComplaintPoint`/`RiverDryPoint` (Mission 2 trigger + 2 context points, reworked fiction — see §9), `Container_Trivial_M1`/`Container_Optimal_M1`, `Container_Trivial_M2`/`Container_Optimal_M2`, `TownHall` (with `blackSmithBase_1`/`blackSmithRoof_1`-style stage sprites), `TrashManager` (hosts `TrashSpawner`), `TrashCollectionSite`, `CoinRewardSystem`, `TrustSystem`, `InventorySystem`/`InventoryUI`, `MissionBoard`, `InfoBoard`, `MinimapCamera`. `PDCAIndicatorUI` is implemented (§10) but not yet wired into this scene.
- **Tuned values:** 8 inventory slots; 2 Gold Coins required to submit a stage; trash spawn interval randomized 25–45s, paused outside Exploration, no numeric penalty on spawn; trust starts at 2/5 per mission, ±1 per outcome; Gold Coin reward is a flat 1 per optimal mission (no per-mission tuning, unlike the old satisfaction rewards).

## 12. Design Rationale Notes (why it's built this way)

- **Quiz-drives-outcome instead of a solution picker** removes the "just pick optimal, it sounds better" meta-strategy a menu invites — the player has to actually reason through causality to earn it, which is the whole point of teaching 5 Whys.
- **All-5-or-trivial (no partial credit tiers)** was a deliberate design choice, not a missed nuance — the note in `PlanningUI`'s design ("hitting all 5 is intentionally hard") signals the team wants root-causing to feel genuinely hard to nail, not a coin-flip.
- **Rejection reopens in place rather than restarting the mission** keeps the loop's cost proportional to the mistake — the player doesn't replay dialogue or re-walk across the map, only re-answers the quiz (now scaffolded) and, for Mission 1, re-solves a puzzle that's been reset to its original layout.
- **Gold Coins only ever reward optimal work, never trivial** — this is what makes the Stage Gate's review flow (§7) simple: there's no reward to retract when a trivial mission gets sent back, because it never earned one. The old satisfaction system needed a `pendingRetraction` flag to avoid double-dipping on a redo; the coin economy doesn't need an equivalent at all, since a wrong outcome just banks nothing instead of banking something that then has to be clawed back.
- **Event bus as the sole coupling layer** is what makes the Stage Gate reset (§7) tractable at all: `OnMissionsNeedReview` reaches five-plus unrelated systems (NPC, river, puzzle, part/waste collection, mission board) without any of them referencing each other or the `StageManager` directly.
- **Match existing structure over "more correct" in the abstract** — when a new feature could reasonably be built more than one way, the codebase prefers whichever way is consistent with how similar things already work, even over an option that's more textbook-correct. The clearest example: NPC trust pips could have used a UI `Image` + World Space `Canvas` (the generically "proper" way to float UI over a world object), but every other world-attached visual in this game is a `SpriteRenderer` on a sorting layer — so trust pips are `SpriteRenderer`s too (§10), keeping "how do I show something above an object in the world" answered one way instead of two. Consistency for future maintainers outranks architectural purity.
