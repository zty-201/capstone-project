# TODO / Planned Features

Mission 4 ("The Tangled Marketplace") is now fully implemented — see `CLAUDE.md` and
`Docs/GameDesignDocument.md` §9 for the current architecture, and
`Docs/Mission4_KanbanSetup.md` for Editor setup/tuning reference. What follows is what's
actually still open.

## 1. Mission 4 cleanup (small, before moving on)

Found during a scene audit, none of these block play, all quick Editor fixes:
- `KanbanBuilderSystem.simDuration` is still at the code default (12s) rather than the
  suggested 20s — Cloth's 6s delivery lead time can't complete a full cycle in 12s during
  testing.
- `KanbanStallGaugeUI.failureOutline` is unwired (`None`) on all 4 gauges — the dedicated
  "this stall failed" visual (specifically needed for a "wasteful" failure, which can look
  perfectly healthy by fill color alone) isn't currently showing up. Needs the
  `FailureOutline` child object built and wired per-gauge.
- `M4KanbanPanel`'s `CanvasScaler` is set to Constant Pixel Size, not Scale With Screen
  Size as the setup doc calls for — this is the confirmed cause of the gauges
  distorting/scaling inconsistently as the game window/resolution changes.
- (Cosmetic only, confirmed non-functional) `M1PipePanel`'s `RectTransform.localScale` is
  `(0,0,0)` — harmless for a Screen Space - Overlay Canvas specifically (Unity ignores a
  root Overlay Canvas's own Transform for rendering/raycasting), but still worth setting to
  `(1,1,1)` for hygiene/future-proofing.

## 2. Post-5-missions Farming & Fishing Sandbox

**Why:** this is a capstone project, not only an educational deliverable — the goal is a
genuinely engaging game, not just a Kaizen tutorial with a village skin. Target split is
roughly **50/50 between the Kaizen campaign and the sandbox**, in development effort and
content breadth — not 30/70 sandbox-dominant, since the core requirement is still
demonstrating the Kaizen/PDCA teaching, and a reviewer should read the finished project as
"a full game that teaches Kaizen," not "a short tutorial bolted onto a different game."

**Structure:** finish all 5 missions (`StageManager.AllStagesComplete`) → unlock farming
and fishing as a sandbox layer on top of the existing village. Framed as "prove you
internalized this, then go apply it," not "sit through the boring part first." Two months
of runway as of 2026-10-01, so this can be scoped properly rather than minimally.

### 2.1 Farming loop

Core loop: till soil → plant a seed (consumes a `Seed` `ItemData`) → wait real in-game
days → harvest (produces a `Crop` `ItemData`) → sell.

- **Growth stages need a time source of their own.** The game no longer has days —
  `OnDayCompleted` was removed along with stages (see `CLAUDE.md`, Rushed-Fix Breakdown).
  A planted tile could store a planted time and swap seed → sprout → ready-to-harvest as
  in-game time passes (paused outside `Exploration`, like `TrashSpawner`'s timer), or the
  sandbox could reintroduce a simple day cycle (e.g. sleeping) that raises a new day event.
- **Selling reuses the existing Gold Coin economy** (`InventorySystem`, `CoinRewardSystem`'s
  pattern) — and should tie back to Mission 4's marketplace fiction directly: sell
  harvested crops at the same marketplace, closing the loop between the Kaizen campaign and
  the sandbox rather than treating them as unrelated.
- **Soft Kaizen callback, not another forced quiz:** replanting the same crop in the same
  plot repeatedly degrades yield over time (soil exhaustion); rotating between 2+ crop
  types sustains yield. Discoverable through play, no popup, no 5 Whys — the instinct the
  first 5 missions taught shows up again without re-teaching it explicitly.
- Considered and deliberately rejected: making this loop itself a 6th mission. The lesson
  (yield degrading over repeated cycles) only manifests across multiple real days, which
  doesn't fit the one-sitting bounded-mission shape every other mission uses. Revisit only
  if a compressed/simulated-time version (same trick `KanbanBuilderSystem.simDuration`
  uses) is wanted as an explicit teaching moment ahead of the real sandbox version — not
  needed if the sandbox's discoverable version is sufficient on its own.

### 2.2 Fishing minigame

A plain `IInteractable` at the riverside (same shape as `TrashCollectionSite`/
`WellPatchSite`) opening a simple timing-bar minigame — stop a moving marker inside a
target zone to catch a `Fish` `ItemData` into inventory. Deliberately simple scope (one
mechanic, no fish variety/rarity tiers) for a first pass; expand only if time allows after
farming is solid.

### 2.3 Reusing Mission 4's retired trivial-path components

`MarketStall.cs`/`MarketStallTrivialSystem.cs` (unused in Mission 4's actual flow since its
conversion to an Advanced Mission — see `CLAUDE.md`'s Mission 4 section) and
`MarketAttendantNPC.cs` (still active, driving `MarketAmbientSystem`'s epilogue) are
earmarked for reuse here:
- `MarketStall`'s stock-ratio-driven visual (already `IInteractable`-ready, just needs a
  `Collider2D` re-added) is a natural fit for "a stall selling the player's own harvested
  crops," rather than building a new sell-point visual from scratch.
- `MarketAttendantNPC`'s dispatch-to-a-destination movement (already decoupled from
  Mission 4 specifically) could represent an NPC buyer or delivery helper in the farming
  loop without new pathfinding code.
