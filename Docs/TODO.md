# TODO / Planned Features

## Mission 4 — "The Tangled Marketplace" (`missionID: 4`) — DRAFT SPEC, not yet implemented

Status: `M4_1.asset` exists in `MissionRegistry`/`Stage2.missionIDs` but is a completely empty
`MissionData` stub (no complaint, root cause, 5 Whys, or reflection text authored). The original
planning document (`Planning Document.docx`) filed this concept under "Advanced Missions" as a
6x6 grid path-clearing puzzle with an efficiency-scoring formula. This draft replaces that
mechanic with a Kanban/pull-system minigame instead — see **Design rationale** at the end for why.

### Complaint
*"Half the stalls sit empty by midday and buyers just walk off — but the other half are drowning
in stock that's gone bad before anyone bought it! Nobody can tell what's actually needed until
it's already too late."* — raised by a Merchant NPC at the marketplace.

### Root cause (5 Whys chain)
1. **Why do buyers walk off empty-handed at some stalls while others waste unsold stock?** →
   Every stall either runs dry mid-day or ends up overstocked with goods that spoil unsold.
2. **Why do stalls run dry or overstock?** → Merchants restock by feel, whenever they happen to
   notice or remember — not based on what's actually being sold.
3. **Why do they restock by feel instead of by need?** → There's no signal that tells a merchant
   exactly when a stall has actually gotten low.
4. **Why is there no such signal?** → Nobody's ever set a reorder point — every order is a bulk
   guess made whenever a merchant thinks to place one.
5. **Why has no one set a reorder point?** → No one's ever separated "how much stock is sitting
   here" from "how much do we actually need right now" — every order tries to guess both at once,
   so it's always wrong in one direction or the other.

**Actual root cause:** there's no pull-based restocking signal — merchants push large speculative
orders instead of pulling exactly enough stock, exactly when it's actually needed. This is a
**Kanban / pull system** problem, not a tidiness problem: the market doesn't need less clutter, it
needs a signal for *when* to restock.

**Design/history note:** this isn't a stretched metaphor — Taiichi Ohno developed the Kanban system
at Toyota in 1953 after directly studying how American supermarkets restock shelves: a supermarket
only pulls more stock once a shelf is actually low, signaled visually, rather than pushing
inventory based on a guess. A marketplace stall is the *original* case study the tool was invented
to solve, which is worth surfacing in the optimal reflection text below.

### Trivial — "Restock by Feel"
**Mechanic:** over a short simulated market day, stalls empty at random intervals; the player taps
"Restock" on whichever stall visibly looks empty, reactively, with no way to anticipate a stall
about to run dry. The sequence always completes (same "trivial always succeeds, just badly"
pattern every other mission's trivial path follows) and fires `RaiseMissionCompleted(4, false)`
once the simulated day ends, regardless of how many stalls sat empty or overflowed with unsold
stock along the way.

### Optimal — "Set Up a Kanban Reorder System"
**Mechanic:** a *configure-then-simulate* puzzle — a new mechanical genre for this game (every
other optimal minigame is either a manipulated-in-real-time puzzle or a fetch/assemble chain; this
one is set up once, then run and observed). Four stalls (Produce, Fish, Tools, Cloth), each with
its own stock gauge, consumption rate, and delivery lead time. The player drags a reorder-point
marker onto each gauge (the "kanban card") — set it too high and the stall wastefully overstocks;
too low and it runs dry before the delivery arrives. Once all four are set, the player runs the
simulated day: each stall's gauge animates in real time, glowing green if it stays in a healthy
band, red if it empties or overflows. A day with every stall staying healthy throughout fires
`RaiseMissionCompleted(4, true)`; a failed run lets the player adjust thresholds and re-run without
resetting the whole minigame, since the "puzzle" is the tuning, not a one-shot input.

### Mission shape: classic, not Advanced
Kept as a **classic** mission (5 Whys `correctCount == 5` directly picks trivial vs. optimal via
`RaiseSolutionSelected`, same as Missions 1 and 2) rather than a third `isAdvancedMission`, so
Stage 2 reads as `[3 advanced, 4 classic, 5 advanced]` instead of three consecutive
diagnosis-doesn't-decide-the-path missions.

### Reflection text
- **Trivial:** *"You keep running back and forth restocking whatever looks empty. Some days it
  works out — other days a stall sits bare for hours, or rots with stock nobody bought in time.
  It's exhausting, and sooner or later you're going to miss one."*
- **Optimal:** *"Each stall now signals the moment it actually needs more — never empty, never
  overflowing. You barely had to watch over any of them; the market keeps itself running exactly
  as it should. Turns out the idea has a name: a Kanban system — and it was invented by watching
  supermarkets do exactly this."*

### Design rationale — departures from the original planning document and prior drafts
- **Mechanic changed from grid path-clearing (original doc) → zone-sort matching (first draft) →
  Kanban pull-system simulation (this draft).** The grid-clearing idea overlapped Mission 1's
  pipe-grid and Mission 2's trivial rubble-clearing; the zone-sort idea, while mechanically clean,
  didn't have a strong reason to be *this* mission's lesson specifically. Kanban does: it's the
  one Lean tool whose textbook origin story *is* a marketplace, so the fiction and the mechanic
  reinforce each other instead of the mechanic being picked first and the fiction fitted around it.
- **New mechanical genre for the game:** every other optimal minigame is either manipulated
  continuously (pipe rotation, bridge building) or a discrete fetch/assemble/place chain (winch,
  brick). This is the first "configure a system, then watch it run" minigame — closer to a light
  management-sim than a puzzle, which adds real variety to the mission roster rather than another
  reskin of drag-and-drop or tap-to-clear.
- **Mission shape changed from Advanced (original doc) to classic** — unchanged rationale from the
  prior draft: avoids three consecutive Pillar-1-breaking missions in Stage 2.
- Per the "Scope of these conventions" note in `CLAUDE.md` and the matching note in
  `GameDesignDocument.md` §12, this spec was chosen purely on design merit (fit to the marketplace
  fiction, mechanical distinctiveness, pedagogical grounding) rather than for resemblance to any
  other mission's implementation shape.
