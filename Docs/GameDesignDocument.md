# Kaizen Village — Game Design Document

*Unity 6, 2D top-down. Compiled from the current build (`Assets/Scripts`, `Assets/Data`, `Assets/Scenes/SampleScene.unity`) as of this writing.*

---

## 1. High Concept

Kaizen Village is a 2D top-down educational game that teaches **Kaizen / PDCA (Plan–Do–Check–Act)** continuous-improvement thinking through village-management vignettes. The player walks around a small farming village, listens to villagers describe a problem, interrogates the problem with a guided **5 Whys** investigation, and is routed — based on how well they diagnosed the root cause, not by free choice — into either a quick patch or a proper structural fix. A minigame executes whichever solution was earned, and a reflection message makes the Kaizen lesson explicit: band-aid fixes don't stick.

The core rhetorical trick of the design: **the player never picks trivial vs. optimal directly.** They pick answers to "why" questions. The system converts diagnostic accuracy into the systemic-vs-superficial outcome. Only a perfect diagnosis (5/5) earns the real fix. This mirrors the pedagogical point — genuine root-cause analysis is hard and mostly-right isn't good enough — better than a menu ever could.

## 2. Pillars

1. **Diagnose, don't choose.** The 5 Whys quiz *is* the decision point. There is no separate "pick a solution" UI. (Advanced Missions repurpose the quiz score into something other than the decision point itself — see §9's Missions 3 and 5 and the rationale note in §12 — but diagnosis still always has to happen before the fix.)
2. **Consequences persist, but aren't punitive dead ends.** A trivial fix isn't a fail state — it's a deferred one. A rushed fix breaks down after the player's next mission (§7) and resurfaces for a redo, instead of blocking progress silently or permanently penalizing the player.
3. **Everything is diegetic where possible.** Progress is a real inventory of Gold Coins the player earns and physically carries to Town Hall to invest in the village, not an abstract score; trash is a physical nuisance that has to be picked up and carried to a collection site, not a number that just decays; upgrades are the village's own buildings visibly improving, not a menu screen.
4. **One state, one responsibility.** Every mode of play (exploring, talking, planning, puzzling, reading a reflection) is an explicit state so input routing never has to guess what the player is currently doing.
5. **No dead-ends from imperfect play.** A wrong 5-Whys answer doesn't block progress mid-quiz (it always advances) and a wrong overall outcome doesn't block anything else (it just withholds full credit and reopens later).

## 3. Player Fantasy & Loop

The player is an unnamed problem-solver dropped into a village where infrastructure keeps failing in ways that look simple on the surface but aren't. The moment-to-moment loop:

```
Explore village → find NPC/problem site → Dialogue → 5 Whys quiz (Planning)
   → system scores diagnosis → routes to Trivial or Optimal minigame
   → minigame resolves → Reflection popup states the Kaizen lesson
   → Mission Board updates → pick any other mission
   → (a rushed fix from earlier breaks down and reopens — see §7)
   → visit Town Hall with enough coins → the whole village upgrades
```

There are no stages or days. Missions can be played in any order, and the session-level structure
is two interlocking tracks: **quality** (the Check — rushed fixes break down and come back until
they're solved at the root, §7) and **investment** (the Act — Gold Coins earned from root-cause
fixes are spent at Town Hall to move every building from rundown → improved → well built, §8).
Buying the final upgrade, which takes all 5 coins and therefore every mission solved optimally, is
the game's ending.

## 4. Core Systems

### 4.1 State Machine
`GameManager` owns a `GameStateManager` holding one `IState` per `GameStateType`. States never talk to each other directly — only through `GameManager.Instance.StateManager.ChangeState()` or the event bus. This keeps input routing unambiguous: whatever `Tick()` is currently active owns the click.

| State | Responsibility |
|---|---|
| `Exploration` | Free roam; click-to-move / click-to-interact via `InputManager` |
| `Dialogue` | Advances NPC/complaint dialogue lines on click |
| `Planning` | 5 Whys quiz UI; ESC returns to Exploration |
| `Puzzle` | Pipe clicks route directly via `IPointerClickHandler` on each Canvas-based `PipeVisual`, not through `Tick()` polling |
| `BridgeBuilder` | Forwards press/hold/release world positions into `BridgeBuilderSystem`'s drag placement; ESC returns to Exploration (guarded — only while still building) |
| `RoutineBuilder` | ESC returns to Exploration; card drag/drop routes directly via Unity's drag-and-drop handlers on the Canvas panel, not through `Tick()` polling |
| `Reflection` | Dismiss the post-mission feedback popup |
| `MissionBoard` | Read-only board overlay; ESC to close |
| `TownNotice` | Full-screen announcement: town upgraded (the final one is the ending) or a rushed fix broke down |
| `InfoBoard` | In-game tutorial/reference pages; ESC to close |
| `SettingsMenu` | BGM/SFX volume sliders; ESC or an on-screen Close button (Android has no ESC-equivalent) returns to Exploration |

### 4.2 Event Bus
A static `EventBus` class of C# events is the *only* coupling mechanism between systems — no domain references another domain's concrete type. Key events: `OnMapClicked → OnPathRequested → OnPathGenerated`, `OnSolutionSelected`, `OnMissionCompleted`, `OnMissionsNeedReview`, `OnReflectionDismissed`, `OnTownUpgraded`, `OnInventoryChanged`, `OnTrustChanged`, `OnPDCAPhaseChanged`. This is what lets, e.g., the river visuals, the inventory HUD, the trust pips, and the mission board all react to a single `OnMissionCompleted` firing without knowing about each other.

### 4.3 Pathfinding & Movement
A* over a `GridSystem` built from a collision `Tilemap`, using a binary min-heap for the open set. `PlayerController` walks paths via coroutine and reroutes around moving NPCs by waiting exactly one frame before recomputing — long enough to avoid a same-frame recursive stack overflow if the NPC is still blocking the new path's first step. NPCs that should physically block a route sit on a dedicated `NPC` trigger layer so they're avoided without ragdoll-style physics response.

`GetRandomWalkableCoordinates` BFS-flood-fills from a start point so patrol/wander targets are always drawn from the *reachable* set — no failed A* search against an unreachable island tile.

### 4.4 Data Layer
All mission content is authored as ScriptableObjects, not hardcoded — a content designer can add a Mission 3 without touching a state machine or minigame script:

- **`MissionData`** — complaint text, root cause, the 5-Whys chain (`WhyStage[5]`: question/correctAnswer/distractors/hint), both solution names, both reflection texts. No longer carries any reward numbers — see §8, Gold Coins are a flat, mission-agnostic reward now.
- **`MissionRegistry`** — flat array of `MissionData`, looked up by `missionID`.
- **`ItemData`** — one inventory item's identity (`itemID`/`itemName`/`icon`) and stacking rules (`stackable`/`maxStack`). Two assets exist: **Gold Coin** (stackable) and **Trash** (not stackable, so litter piles up one slot per piece instead of quietly stacking away).

## 5. The 5 Whys Mechanic (the game's signature system)

Run by `PlanningUI` after it types out both solution names for flavor. Five sequential "Why" stages, each a multiple-choice pick among the correct answer and its distractors.

**Rules:**
- Picking *any* option always advances — there's no hard-blocking retry loop mid-quiz. This keeps pacing snappy; punishment is deferred to the outcome, not friction injected into every question.
- `PlanningUI` tallies `correctCount` across all 5 stages.
- `outcomeIsOptimal = (correctCount == 5)`. Anything less routes to the trivial fix. This is deliberately unforgiving — one slip anywhere in the chain denies full credit, which is the game's thesis: surface-level root-causing isn't root-causing.
- That boolean is what feeds `RaiseSolutionSelected(missionID, outcomeIsOptimal)`, which `MinigameActivator` uses to activate the correct minigame container and switch state.

**Redo behavior differs from a first attempt** (driven by Rushed-Fix Breakdown, §7):
- `hintText` is suppressed on a first attempt and only shown once `MissionReviewSystem.IsMissionUnderReview(missionID)` is true — so a first pass is a genuine cold diagnosis, and a forced redo gets scaffolding rather than repeating the same blind guess.
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

## 7. Rushed-Fix Breakdown (the retention/replay layer)

This is the mechanism that keeps a "good enough" trivial fix from quietly counting as done — without ever hard-blocking the player from continuing to explore and act on other missions.

**A trivial resolution is a rushed fix, and rushed fixes don't last.** The next time the player completes *any other* mission, every rushed fix still standing breaks down: the well starts leaking again, the river jams again. A notice announces it ("A Quick Fix Gave Way"), and the mission reopens in place for a redo. If the rushed mission is the only one left unsolved, it breaks down straight away, so the player can never get stuck. `MissionReviewSystem` runs this, and only announces breakdowns once the player closes the reflection popup — the quiet moment after a mission, not in the middle of it.

**Why this replaced stages.** The game used to group missions into stages, each submitted at Town Hall: every mission optimal *and* no trash on the ground, or the trivial ones got sent back. Once coins moved to town upgrades (§8) and missions became playable in any order, stages were mostly extra rules — and the no-trash requirement felt like a bug whenever a piece spawned just as the player walked up to submit, which they couldn't prevent. Breakdown keeps the one thing stages did that mattered (rushed work comes back) and improves on it:
- **It happens in the world, not at a desk.** It's the core Kaizen lesson made literal: band-aid fixes don't hold.
- **It's paced, not spammable.** A retry only opens after the player has done something else, so a redo is a considered second look rather than an immediate re-roll.
- **Trivial completions never earned a Gold Coin**, so there's nothing to claw back when a fix breaks down.

**`OnMissionsNeedReview` puts the broken-down mission back to its pre-completion state in place** — no scene reload, no re-walking to a checkpoint:
- `NPCController` clears its completed flag and re-shows its interaction indicator.
- `RiverInteractable` re-activates itself.
- `PipePuzzleSystem` resets every pipe to its *cached original* rotation/bitmask.
- `PartCollectionSystem` / `WastePickupSystem` reset collected counts and re-show pieces.
- `MissionEntryUI` un-greys.

Components that live inside a `MinigameActivator` container that gets *disabled* on completion subscribe to `OnMissionsNeedReview` in `Awake`/`OnDestroy` rather than `OnEnable`/`OnDisable` — an `OnEnable` subscription would already be torn down by the time a review request (which can only fire after completion) needs to reach a disabled object.

The redo then runs through the *same* 5 Whys quiz, with the hint/distractor-exclusion scaffolding from §5 active. This is the design's actual "Check → Act" loop made mechanical: the fix fails in the world, the player gets a scaffolded second attempt.

## 8. Gold Coin Economy & Inventory

There's no abstract progress meter anymore — `TownSatisfactionSystem`/`SatisfactionBarUI` were
removed outright and replaced with a real inventory the player physically carries, in service of
Pillar 3 (diegetic feedback over HUD abstraction).

**`InventorySystem`** (singleton) owns a fixed array of 8 `InventorySlot` (plain `ItemData item` +
`int count`, not a `MonoBehaviour`). `TryAddItem` stacks into an existing slot when the item is
stackable and there's room, otherwise claims the first empty slot, and returns `false` if nothing
fits. **Slot 0 is reserved for Gold Coins** (`reservedSlotItem`): nothing else may occupy it and
coins only ever go there, so a coin can never be lost because trash filled the inventory at the
moment a mission awarded it — once coins are a real budget, losing one that way would feel unfair. Every mutation fires `OnInventoryChanged` (no payload — subscribers just re-read `Slots`).
`InventoryUI` is a fixed array of slot `Image`/count-text pairs that refresh on that event,
occupying the screen position the old satisfaction bar used to hold.

**Earning coins:** `CoinRewardSystem` listens to `OnMissionCompleted` and awards exactly 1 Gold
Coin — but only when `wasOptimal`. A trivial completion earns nothing, which is deliberate: it
means there's nothing to claw back later if that mission gets flagged for review and reattempted
(see §7) — the coin count is always an honest, un-gameable record of missions actually solved at
the root cause. The Gold Coin `ItemData` is stackable, so every coin the player is carrying lives
in a single slot.

**Spending coins — the town upgrade (the "Act" step):** coins are the village's improvement
budget, spent at Town Hall via `TownUpgradeSystem`. Every building starts rundown; **3 coins** buy
the first upgrade and **2 more** (all 5 in the game) buy the final one. Each purchase raises
`OnTownUpgraded(level)` and every building with a `BuildingUpgrade` swaps to that tier at once.
- *Why it's the progression spine:* it gives the player a visible goal they choose when to cash
  in, and since trivial fixes earn no coin, a player who takes shortcuts *sees* the town lag
  behind — the lesson shows up in the world, not only in reflection text. The final upgrade is
  the ending.
- *Tier effects:* each upgrade also slows trash spawning (×2 interval at tier 1, ×4 at tier 2 —
  `TrashSpawner.intervalMultiplierPerTownLevel`), the 5S idea that a sorted, well-kept town stays
  clean. Tier 2 is only affordable once every mission is solved optimally, so its effect is
  deliberately a post-missions reward rather than something that helps with missions.
- *Rejected alternative:* per-building upgrades chosen by the player. With only 5 coins there are
  too few picks for real strategy, most playthroughs would end with part of the town still
  rundown (undercutting the final payoff), and it needs a picker UI.

**Trash** is now something the player physically carries rather than a satisfaction penalty.
`TrashSpawner` periodically spawns a piece at a random unoccupied point — spawning is purely
presence-based now, no numeric penalty on spawn, and it still pauses outside `Exploration` so
nothing punishes the player for being mid-dialogue or mid-minigame. `TrashPiece.Interact()` tries
to add itself to the inventory (the Trash `ItemData` is **not** stackable, so every piece claims
its own slot — letting litter pile up crowds out mission items, though never Gold Coins, which have their reserved slot); on
success it's removed from the ground, on failure (inventory full) it's left untouched rather than
lost. **`TrashCollectionSite`** is a plain interactable placed in the village — one interact clears
every Trash slot in the inventory at once. Trash gates nothing — no submission or check
waits on clean streets (that requirement was removed with stages, §7) — its cost is purely
inventory pressure on the fetch/collect missions, eased by each town upgrade.

## 9. Missions

### Mission 1 — "The Parched Crops" (`missionID: 1`)
**Complaint:** *"There's not enough water to water the crops!"* — raised by the `Farmer_NPC`.
**Root cause:** the central well's stone lining is cracked from age with no reinforcement, leaking water into the soil before it reaches the surface.

| | Trivial | Optimal |
|---|---|---|
| Name | *Patch the bucket and rope* | *Rebuild the pulley and pipe filter system* |
| Mechanic | Two-stage fetch quest through the real inventory (`BrickPickup` → `WellPatchSite`) | `Container_Optimal_M1` — a full 5×5 rotate-the-pipes puzzle (`PipePuzzleSystem`), not every cell filled |
| Reflection | *"The patch worked for now, but the root cause remains. The well will fail again soon..."* | *"Great work! Rebuilding the pulley system fixed the root cause. Water flows properly now."* |

**Pipe puzzle mechanics:** `PipeDirection` is a `[Flags]` bitmask (Up/Right/Down/Left). Each `PipeVisual` reads its `PipeShape` + authored rotation to compute a starting bitmask; clicking rotates it clockwise via `(bits << 1 | bits >> 3) & 15`. The puzzle board is a Canvas panel now (not world-space), and clicks route directly through `IPointerClickHandler` on each pipe — **position-based, not adjacency-gated**, since the puzzle is a fixed board the player looks at rather than a thing they walk up to piece-by-piece. After every rotation, a DFS flood-fill checks for a connected water path from `startPos` (0,0) to `endPos` (2,2); a valid path fires `RaiseMissionCompleted(1, true)`. Original per-tile bitmask/rotation is cached at scene start (including from *inactive* pipes, since this container may never activate if the player earns the trivial path instead) so a Stage-Gate reset can restore the exact starting board.

**Design note:** the well used to have a bespoke `PatchWellState` that blocked player movement entirely. It was removed because the triggering NPC can wander (via `NPCPatrol`) away from the well before the player finishes dialogue, which could strand the player in a state where nothing was reachable. Folding the trivial path into ordinary `Exploration` + `InputManager`'s walk-then-interact routing fixed that for free, since a click on `WellVisual` doesn't need the puzzle's no-adjacency click model anyway.

### Mission 2 — "The Blocked River" (`missionID: 2`)
**Complaint:** *"The river's gone still and green — we wash and draw drinking water from there, and some of us have gotten sick."*
**Root cause:** the cliff face above the falls has been quietly eroding for seasons; a rockslide has jammed a boulder at the lip of the falls, and every past slide has only ever been shoved aside by hand — nothing was ever built to clear one safely and keep the channel clear, so the river keeps losing its fresh inflow and stagnating.

The map's river now runs from a cliff-top source down a waterfall and on through the village —
there's no pond; it's the river itself, jammed at the falls, that's gone stagnant. This is a
rework of the original flat-riverside "clogged river" fiction to match the renovated map art
(cliff → falls → river channel). The underlying mechanics are unchanged; only the fiction and
`MissionData` content were re-skinned, per the "match existing structure" convention (see
`CLAUDE.md`) — reuse what already works rather than build a parallel system for what is fictively
a new scenario.

Unlike Mission 1, the trigger is **not** an NPC — it's `RiverInteractable` sitting directly on the
boulder wedged at the lip of the falls (a natural rockslide, not litter or a human cause). Two
additional `ContextInteractable` points nearby (the thinned-out riverbed below the falls,
villagers warning that the river water is unsafe to drink or wash in) are pure narrative flavor:
they show dialogue and return straight to Exploration without touching any `MissionData` or
mission state, giving the player context before they ever open the 5 Whys quiz.

| | Trivial | Optimal |
|---|---|---|
| Name | *Clear the loose rubble* | *Rig a cliffside winch* |
| Mechanic | `WastePickupSystem` + N `WastePiece` interactables overlapping rubble shaken loose by the slide; each click hides its visual and decrements a counter | `PartCollectionSystem` + 3 fixed `MachinePart` pickups → `AssemblyPoint` (assemble the winch) → `PlacementPoint` at the cliff lip (anchor it) |
| Reflection | *"You've cleared enough loose rock for a trickle to get through — the river stirs a little, but nowhere near enough to flush out the stagnant water. This will happen again."* | *"With the rig anchored at the lip, you finally lever the wedged stone free. The falls roar back to full flow, flushing the river clean — and the rig stays bolted in place to catch whatever comes down next."* |

Both paths ultimately fire `RaiseMissionCompleted(2, wasOptimal)`, which `RiverManager` listens for regardless of which path was taken: it disables the `blockageVisual` (the wedged boulder) and enables `animatedRiverTilemap` either way — the *visual* payoff (falls flowing again) is identical, deliberately, so the game doesn't spoil "this was the wrong fix" before the reflection text says so.

**Optimal path is still the game's clearest Jidoka example** (a mechanism that keeps the fix running automatically instead of a human repeating the same manual chore — here, literally standing guard against the *next* rockslide) — the reflection text implies the Lean principle through what the rig actually does, even without naming it outright, tying the fiction back to the pedagogy.

### Mission 3 — "The Farmer's Broken Routine" (`missionID: 3`)
**Complaint:** *"The clumsy farmer waters crops at random times and forgets steps constantly — the whole town feels the small inefficiencies."*
**Root cause:** there's no standardized routine for the farm's daily stations — the order chores get done in is left to chance, so earlier steps keep getting undone by later ones.

Mission 3 is the game's second **Advanced Mission** (see Mission 5 below and the rationale note in §12): the 5 Whys quiz still runs and still matters, but its score no longer decides trivial-vs-optimal directly — there's only one minigame, and how the player actually performs at it decides the outcome, same shape as Mission 5.

**The minigame is a drag-to-reorder puzzle, and it's the game's most direct dramatization of Kaizen's Standardized Work principle** — the 5 Whys chain for this mission ends at "nobody treats the farm's daily workflow as something worth standardizing," and the fix *is* literally standardizing that workflow: the player arranges 4 daily stations (feed the animals, water the crops, harvest the crops, sell at market) into a working order and submits it. Two orders are accepted — feeding the animals and watering the crops don't depend on each other, so either can come first — but harvesting has to follow watering, and selling has to follow harvesting; get that dependency wrong and the day doesn't work, the same way the earlier missions' root causes get "undone" by acting out of the wrong order.

The player gets 5 attempts (plus bonus attempts for a strong 5 Whys diagnosis — same "quiz score becomes practice attempts, not the decision itself" idea Mission 5 uses). Running out without finding an accepted order resolves the mission trivially and reopens it for a Stage Gate redo, same as every other mission's fail path.

**Reflection:** *(trivial)* "The farmer never quite settles into a rhythm — some days the harvest sits too long before it ever reaches market, other days the animals go hungry. Villagers shrug it off as just how he is." *(optimal)* "With every station tackled in the right order, the farmer's day finally clicks into place — nothing sits waiting, nothing gets skipped, and a full cart makes it to market every time."

**Status: the per-submission payoff is still under construction.** The design calls for "a small CG to show the NPC executing the tasks" on every submitted attempt — what's implemented today is a lightweight placeholder (each station briefly highlights in the order submitted, then a pass/fail message), not the animated cutscene. The ordering/scoring logic doesn't depend on how that moment is visualized, so the real CG can be dropped in later without touching anything else about the mission.

### Mission 4 — "The Tangled Marketplace" (`missionID: 4`)
**Complaint:** *"Half the stalls sit empty by midday and buyers just walk off! But the other half are drowning in stock that's gone bad before anyone bought it — nobody can tell what's actually needed until it's too late."*
**Root cause:** there's no pull-based restocking signal — merchants push large speculative orders instead of pulling exactly enough stock, exactly when it's actually needed.

Mission 4 is the game's third **Advanced Mission** (same shape as Mission 3/5, see the rationale note in §12): one minigame, no separate trivial-path container, and the 5 Whys score becomes bonus attempts rather than picking the path.

**The minigame dramatizes Kanban — a real Lean/Kaizen tool, not an invented metaphor.** Taiichi Ohno developed Kanban at Toyota in 1953 after directly studying how American supermarkets restock shelves: a shelf only gets pulled from once a customer actually takes something, never restocked speculatively. A marketplace stall running dry or drowning in unsold stock is the *original* case study the tool was built to solve. The player sets a reorder-point threshold per stall, then runs a simulated market day: each stall drains at its own pace, and a delivery lands only after a real lead time once the threshold is crossed. Set the threshold too low and the stall runs dry during the wait; set it too high and the stall fails as "wasteful" — always carrying extra stock "just in case" is exactly the inventory waste Kanban exists to eliminate, not a safe fallback. Passing requires every stall to clear both failure modes in the same run.

**Attempts** work exactly like Mission 3/5: 5 base tries plus 1 bonus per correct 5-Whys answer, so a strong diagnosis buys more room to actually tune the thresholds correctly.

**Reflection:** *(trivial, attempts exhausted)* "No matter how you adjusted the signals, they never quite held up — a stall would run dry one day and drown in stock the next. After one bad day too many, the merchants gave up on the system and went back to just restocking whatever looked empty, the way they always had." *(optimal)* "Each stall now signals the moment it actually needs more — never empty, never overflowing. You barely had to watch over any of them; the market keeps itself running exactly as it should. Turns out the idea has a name — a Kanban system — and it was invented by watching supermarkets do exactly this."

**Design note:** Mission 4 was originally scoped as a *classic* mission — a real-time "Restock by Feel" trivial minigame the player could tap through, versus this Kanban panel as the optimal path. It was converted to this single-minigame Advanced shape for the same reason as Mission 3/5 (§12) — a quiz can't stand in for "did you actually tune it correctly" any better than it can for "does the bridge hold" — and because the old trivial path ran a real-time clock gated only on general exploration, meaning a player who wandered off to explore the rest of town could come back to a mission that had silently resolved without them ever seeing it happen. The old trivial-path components remain in the codebase, earmarked for the post-5-missions farming/market sandbox (see `Docs/TODO.md`) rather than deleted outright.

A permanent, post-completion epilogue (`MarketAmbientSystem`, see `CLAUDE.md`) keeps the marketplace visibly living out whichever fix actually stuck — an unmanaged village worker restocking at random if the mission resolved trivially, or a dispatched attendant reacting to the player's own tuned thresholds if it resolved optimally — the same diegetic-consequence instinct Mission 2's river reveal and the Town Hall upgrades already use, just sustained indefinitely instead of a one-time visual swap.

### Mission 5 — "The Broken Bridge" (`missionID: 5`)
**Complaint:** *"The bridge to the eastern fields is falling apart. Nobody dares cross it anymore!"*
**Root cause:** the bridge was thrown together as a bare-minimum span, with no triangulated bracing to spread real loads across its planks — a rushed job built to cross a gap, not to carry the weight it would actually see.

Mission 5 is the game's first **Advanced Mission** (`MissionData.isAdvancedMission`), and it deliberately breaks the shape every other mission follows: there is no separate trivial-path minigame to route into. The 5 Whys quiz still runs in full — the same "why does it sway" → "planks aren't braced" → "rushed minimum build" → "nobody planned for real loads" → "no triangulated support structure" chain every other mission uses to teach root-cause thinking — but its score no longer decides *which* minigame the player gets routed to. Instead it feeds bonus attempts into the one minigame everyone plays: a genuine bridge-engineering puzzle where the *test itself*, not a quiz answer, decides whether the fix was superficial or real.

**The minigame — a simplified Poly Bridge.** The player gets a materials budget and two fixed anchor points across a gorge, and has to freely place joints and beams to actually span it, then send a test cart across to prove the structure holds:
- **Materials are a genuine cost-vs-strength tradeoff** — cheap, weak Wood; a solid all-round default; an expensive, strong option — so "just spend more" isn't a free win; the player has to reason about where the real load actually needs bracing, the same underlying root-cause instinct as Missions 1 and 2, just expressed as an engineering budget instead of a diagnosis.
- **Road vs. Reinforcement** — one material is purely structural (a brace the test cart never physically touches, only ever felt through the structure carrying its share of the load), the rest are drivable road. This mirrors real bridge engineering: some members carry load without ever being the surface anything drives on.
- **The tradeoff is always visible, never discovered by trial.** Each material button shows its cost and strength, the budget reads "spent / total", a dot grid shows exactly where a new joint can land, and during the test a plank near its limit both reddens and flashes (so the warning doesn't rely on color alone). The puzzle should be about reasoning where load goes, not about decoding the interface.
- **Passing the test *is* the root-cause fix.** A cart that makes it across proves the structure is properly triangulated and braced — that's what "rebuild it properly" looks like for a bridge, expressed as a real physics test rather than a correct multiple-choice pick.
- **Running out of test attempts is the trivial outcome** — not a separate, deliberately-weaker minigame the player got routed into for answering wrong, but the *same* structure genuinely failing under load, as many times as the player's 5-Whys score bought them attempts to get it right. The quiz score still matters — a stronger diagnosis earns more chances to actually fix the engineering — it just no longer pre-decides the ceiling before the player has touched the puzzle at all.

**Reflection:** *(trivial)* "The design never held up under the test cart's weight, so the crew threw together a quick plank crossing instead. It creaks with every step, and it's only a matter of time before it gives way again." *(optimal)* "The reinforced bridge holds firm under real weight. Properly braced, it'll carry carts and livestock for years to come." Same band-aid-vs-root-cause framing every other mission's reflection carries — earned here by an actually-working structure instead of a dialogue choice.

Presented as a full-screen popup so the player can focus on the structural puzzle without village distraction underneath it — the same instinct behind treating the pipe puzzle as its own focused space in Mission 1, reached architecturally the opposite way round since this puzzle is real physics, not a fixed rotate-the-pipes board (see `CLAUDE.md` for the implementation).

## 10. Ancillary Systems

### Mission Board
One `MissionEntryUI` per mission, Inspector-assigned. On completion, the entry greys (`alpha 0.4`) and reads **"Resolved"** (optimal) or **"Needs Review"** (trivial). `NPCController.HandleSolutionSelected` sets a `missionCompleted` no-op flag and hides the NPC's `InteractionIndicator` on selection — but the NPC GameObject stays active and, if it has `NPCPatrol`, keeps wandering. A "Needs Review" entry can *only* be reopened by its rushed fix breaking down (§7) — there's no manual "redo mission" button, which keeps a retry something that happens to the player after they've moved on, rather than something they can trivially spam.

### Info Board
A walk-up tutorial panel (architectural clone of the Mission Board: `InfoBoardInteractable` → `InfoBoardUI` + `InfoBoardState`, ESC-only). A static, paged reference (`InfoPage[]`, Next/Previous buttons) covering: Welcome, Getting Around, Talking to Villagers, The 5 Whys, Missions & the Mission Board, The PDCA Cycle, Gold Coins & Trust, Trash & Your Inventory, Town Hall & New Days, What You'll Find Around Town. Exists so the game can explain its own mechanics diegetically instead of a forced onboarding sequence.

### NPC Trust Meter
`TrustSystem` (singleton) tracks a `0..maxTrust` (default 5, starting at 2) trust value per
`missionID`: `+1` on an optimal resolution, `-1` on a trivial one, clamped, firing
`OnTrustChanged(missionID, newTrust)`. It's intentionally **visual-only** — trust reflects mission
outcome history but doesn't gate anything; reattempting a trivial mission is handled entirely by
Rushed-Fix Breakdown (§7). Trust persists for the whole game, since it's a standing relationship
signal rather than per-attempt bookkeeping. `NPCTrustUI` renders it as a row
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

### Town Upgrade Tiers
`BuildingUpgrade` sits on every upgradable building and listens for `OnTownUpgraded(level)`, activating the matching child tier (0 = rundown, 1 = improved, 2 = well built). Each tier can be built from separate Base/Roof sprites on `EntityTilemap`/`ForeGroundTilemap` sorting layers so the roof still renders in front of the player while the base renders behind — town progress is legible from across the map without breaking depth sorting. (It replaced `TownHallUpgrade`, which only covered the town hall and swapped by *day* index — the first stage pass raised day 2 and so skipped straight past the middle tier.)

### Minimap
A second camera (`MinimapCamera`) tracks the `Player` tag every `LateUpdate` and renders to a `RawImage` pinned top-right — a live, zoomed-out view of the same scene rather than an icon-based abstraction, consistent with the game's preference for diegetic feedback over HUD abstraction.

### NPC Patrol
`NPCPatrol` wanders NPCs between random walkable tiles, but only while the global state is `Exploration` — NPCs freeze mid-step the instant dialogue or a minigame opens, so nothing looks like it's sliding around behind a modal panel. Patrol targets come from the same BFS-reachability-checked `GetRandomWalkableCoordinates` the pathfinding system exposes, so an NPC never picks a target isolated by unwalkable tiles.

### Settings Menu
A persistent HUD button (reachable from anywhere, unlike the walk-up-triggered Mission/Info Boards) opens `SettingsMenuUI`, currently just BGM/SFX volume sliders backed by `AudioManager`/`PlayerPrefs` so the choice survives a restart. Closing has two paths on purpose: ESC via `SettingsMenuState` for desktop, and an on-screen Close button for Android, which has no ESC/back-key equivalent to fall back on. Opening reads `AudioManager`'s current volume back into the sliders (`SetValueWithoutNotify`, so it doesn't loop back into the volume-change handlers) rather than resetting them to a fixed default every time.

## 11. Content Inventory (current scene)

- **Missions are unordered** — no stages. Missions 1–2 are classic (the quiz picks the path);
  **`3`** ("The Farmer's Broken Routine"), **`4`** ("The Tangled Marketplace"), and **`5`** ("The
  Broken Bridge") are Advanced Missions. A player may start with an Advanced one; the quiz still
  runs first, and a weak diagnosis just means fewer bonus attempts and a likelier rushed fix that
  comes back later with hints. Three Advanced missions in a row was a tradeoff accepted when
  Mission 4 was converted (see §9's Mission 4 design note), on the reasoning that the five missions
  are no longer intended to be the entire game once the post-5-missions farming/market sandbox
  ships (see `Docs/TODO.md`).
- **Missions authored:** `M1_ParchedCrops` (well/farm), `M2_CleaningRiver` (asset name predates the "Blocked River" rework in §9 — content and 5-Whys chain updated in place, filename unchanged), `M3_BrokenRoutine` (see §9 — full 5-Whys chain, `isAdvancedMission` checked, per-submit CG still a placeholder), `M4_KanBanMarket` (see §9 — full 5-Whys chain, `isAdvancedMission` checked; asset renamed in-place from the original `M4_1` stub), `M5_BrokenBridge` (see §9 — full 5-Whys chain, `isAdvancedMission` checked) — all five have complete 5-Whys chains and their solution path(s) implemented and wired in-scene.
- **Notable scene objects:** `Farmer_NPC` (Mission 1 trigger, patrols), `RiverBlockagePoint`/`VIllagerComplaintPoint`/`RiverDryPoint` (Mission 2 trigger + 2 context points, reworked fiction — see §9), `Container_Trivial_M1`/`M1PipePanel` (renamed in-scene from `Container_Optimal_M1`), `Container_Trivial_M2`/`Container_Optimal_M2`, `Container_Optimal_M3` (the routine builder's single minigame container — a Canvas panel, not world-space; see `CLAUDE.md`), `Merchant_NPC` (Mission 4 trigger) and `M4KanbanPanel` (the Kanban panel's single container, renamed in-scene from `Container_Optimal_M4`; Mission 4's original two-container "Restock by Feel" trivial path was removed from the scene entirely when the mission converted to the Advanced shape — see §9), `Container_Optimal_M5` (the bridge's single minigame container, holding the anchor nodes, plank/node prefabs, the test cart, and the second popup-framing camera), `TownHall` (the upgrade shop; with `blackSmithBase_1`/`blackSmithRoof_1`-style tier sprites), `TrashManager` (hosts `TrashSpawner`), `TrashCollectionSite`, `CoinRewardSystem`, `TrustSystem`, `InventorySystem`/`InventoryUI`, `MissionBoard`, `InfoBoard`, `MinimapCamera`. `PDCAIndicatorUI` is implemented (§10) but not yet wired into this scene.
- **Tuned values:** 8 inventory slots (slot 0 reserved for Gold Coins); town upgrades cost 3 then 2 Gold Coins; a rushed fix breaks down after 1 other completed mission; trash spawn interval randomized 25–45s (×2 after the first town upgrade, ×4 after the final), paused outside Exploration, no numeric penalty on spawn; trust starts at 2/5 per mission, ±1 per outcome; Gold Coin reward is a flat 1 per optimal mission (no per-mission tuning, unlike the old satisfaction rewards).

## 12. Design Rationale Notes (why it's built this way)

- **Quiz-drives-outcome instead of a solution picker** removes the "just pick optimal, it sounds better" meta-strategy a menu invites — the player has to actually reason through causality to earn it, which is the whole point of teaching 5 Whys.
- **All-5-or-trivial (no partial credit tiers)** was a deliberate design choice, not a missed nuance — the note in `PlanningUI`'s design ("hitting all 5 is intentionally hard") signals the team wants root-causing to feel genuinely hard to nail, not a coin-flip.
- **Rejection reopens in place rather than restarting the mission** keeps the loop's cost proportional to the mistake — the player doesn't replay dialogue or re-walk across the map, only re-answers the quiz (now scaffolded) and, for Mission 1, re-solves a puzzle that's been reset to its original layout.
- **Gold Coins only ever reward optimal work, never trivial** — this is what makes rushed-fix breakdown (§7) simple: there's no reward to retract when a trivial mission breaks down, because it never earned one. The old satisfaction system needed a `pendingRetraction` flag to avoid double-dipping on a redo; the coin economy doesn't need an equivalent at all, since a wrong outcome just banks nothing instead of banking something that then has to be clawed back.
- **Event bus as the sole coupling layer** is what made replacing stages with breakdown cheap (§7): `OnMissionsNeedReview` reaches five-plus unrelated systems (NPC, river, puzzle, part/waste collection, mission board) without any of them referencing each other or whatever raises it — so only the trigger changed, not a single reset.
- **Match existing structure over "more correct" in the abstract** — when a new feature could reasonably be built more than one way, the codebase prefers whichever way is consistent with how similar things already work, even over an option that's more textbook-correct. The clearest example: NPC trust pips could have used a UI `Image` + World Space `Canvas` (the generically "proper" way to float UI over a world object), but every other world-attached visual in this game is a `SpriteRenderer` on a sorting layer — so trust pips are `SpriteRenderer`s too (§10), keeping "how do I show something above an object in the world" answered one way instead of two. Consistency for future maintainers outranks architectural purity. **This is a code-architecture convention only** — it governs *how* an already-chosen design gets implemented, not *what* the design should be. Brainstorming a new mission or mechanic should be judged on its own design merits (does it teach its Kaizen concept well, is it fun, is it mechanically distinct from other missions) rather than on how closely it resembles an existing mission's shape.
- **Missions 3, 4, and 5 deliberately break Pillar 1 ("the quiz *is* the decision point"), and that's the point of calling them Advanced Missions rather than quietly making an exception.** For a bridge, a multiple-choice diagnosis can't actually stand in for "does the structure hold" the way it can for "did you pick the well-cleaning approach that addresses the root cause" — an engineering fix is either load-bearing or it isn't, and the most honest way to test that is to actually build it and load-test it, not answer a question about it. Mission 3 makes the same call for a different reason: "get the order of daily chores right" isn't a diagnosis you can multiple-choice your way through either — it's a sequencing skill you either demonstrate by actually arranging the stations correctly or you don't. Mission 4 makes the same call for a third reason, discovered mid-development rather than planned from the start: it was originally built as a classic mission with a separate real-time trivial minigame, but that minigame's background clock could resolve the mission while the player was off exploring elsewhere — converting it to the Advanced shape removed that failure mode at the root, on top of the same "a quiz can't prove the tuning is actually correct" reasoning the other two Advanced Missions already use. In all three cases the quiz still runs (root-cause diagnosis is still practiced every time), but its score is repurposed into bonus attempts rather than pre-selecting the outcome — the minigame's own pass/fail becomes the "did you actually fix the root cause" check. This keeps the *spirit* of Pillar 1 (reasoning has to happen before the fix, and a bad diagnosis costs you something concrete) while dropping the specific mechanism (quiz score *is* the outcome) for the mission types where that mechanism would have been a worse simulation of the real lesson, not a better one. Stage 2 (`[3, 4, 5]`) being uniformly Advanced-shaped as a result is a known, accepted tradeoff — see §11.
