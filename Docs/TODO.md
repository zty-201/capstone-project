# To Do

## Mission Directory HUD (top-left objective tracker)

A Canvas UI element, top-left of the screen, showing a compressed one-line-per-active-mission
objective — similar to Zenless Zone Zero's mission tracker. Two lines at game start, e.g.:

```
1. The villager is concerned — look for him in town to ask what's happening.
2. There's a stench coming from the pond — go look at what happened.
```

**The text must track the player's progress *within* a mission, not just whether it's done.**
Given example for Mission 2 optimal path, as the player advances:
```
2. There's a stench coming from the pond — go look at what happened.
2. Collect the parts to build the machine (0/3)
2. Go to the blacksmith to assemble the parts.
2. Place the machine near the blocked river.
```

### Why this isn't a small hook-in
Every other reactive UI in this game (`PDCAIndicatorUI`, `MissionBoardUI`) keys off coarse,
mission-level state (`PDCAPhase`, `wasOptimal`/`missionCompleted`) or a single event firing once
per mission. This feature needs **sub-stage granularity within a single mission's Do phase** —
e.g. "0/3 parts" vs "assemble" vs "place" are all still `PDCAPhase.Do` and all still the same
`missionID`, so neither existing signal can distinguish them. This will likely need either:
- A new small event (`EventBus.OnObjectiveTextChanged(missionID, string)` or similar) that each
  relevant script raises at each sub-stage transition (`PartCollectionSystem.OnPartCollected`,
  `AssemblyPoint.Interact`, `PlacementPoint.Interact`, etc.) — the most consistent option with
  Event Bus as the sole coupling layer, but means a raise-point in ~10+ existing scripts.
- Or per-mission objective text authored as data (`MissionData` gains an ordered list of
  objective strings) with each sub-stage script advancing an index rather than owning its own
  string — keeps content out of code, more consistent with the existing `MissionData` /
  `ScriptableObject` convention, but needs a clear definition of what counts as a "sub-stage
  index" per mission (Mission 1's two paths and Mission 2's two paths don't have the same shape).

### Also needs deciding
- What shows before a mission's ever been started (its intro line, per the two examples given) —
  presumably sourced from `MissionData` too.
- What happens to a line once a mission resolves (trivial vs. optimal) — does it disappear, show
  a "Resolved"/"Needs Review" state matching the Mission Board, or something else?
- How this interacts with the Stage Gate System's reopen-in-place flow (§7 in the GDD) — a
  reopened trivial mission needs its objective text to reset back to the right sub-stage too.
- Follows "match existing structure" (see `CLAUDE.md`): decide the mechanism above by asking
  which existing pattern (event bus vs. data-driven) this is closer to, not by introducing a third
  new pattern.
