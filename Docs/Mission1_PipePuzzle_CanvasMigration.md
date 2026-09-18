# Mission 1 (Pipe Puzzle) — Canvas Migration Guide

The pipe puzzle used to be world-space (`Container_Optimal_M1` + `CameraFollower`,
pipes as `SpriteRenderer`/`BoxCollider2D` prefabs, clicks resolved via a
broadcast `EventBus.OnPuzzleClicked` world position + each pipe's own
`Collider2D.OverlapPoint` check). It's now a Canvas panel — same "separate
Canvas encapsulating the UI" shape Mission 5's `BridgeCanvas` already uses.
This is a migration of an **existing, working, already-tuned** system, not a
fresh build like Mission 3 — treat the steps below carefully, especially the
canonical-bits rotation calibration note in step 2.

## What changed in code (already done)

- `PipeVisual.cs` — `SpriteRenderer` → `Image`, `BoxCollider2D` removed,
  implements `IPointerClickHandler` instead of subscribing to
  `EventBus.OnPuzzleClicked`. **`GetStartingBits()`'s canonical-bits-per-shape
  switch and rotation math are completely unchanged** — they only ever read
  `transform.eulerAngles.z`, which an `Image`'s `RectTransform` reports
  identically to a world-space `Transform`. Nothing about the calibration
  (including the `TJunction` fix `CLAUDE.md` documents) needed to change.
- `PuzzleState.cs` — no longer polls/forwards pointer positions; it's now
  ESC-only, same shape as `MissionBoardState`. Unity's own
  `EventSystem`/`GraphicRaycaster` calls `PipeVisual.OnPointerClick` directly
  on whichever pipe is actually under the cursor — there's nothing left for
  this state to resolve.
- `EventBus.OnPuzzleClicked`/`RaisePuzzleClicked` — removed. Nothing else in
  the codebase referenced them (verified).
- `PipePuzzleSystem.cs` — **unchanged**. It never depended on
  `SpriteRenderer` specifically, only on `PipeVisual.gridX/gridY`/
  `GetStartingBits()`/`SetPowered()`/`ResetRotation()`, all of which still
  work identically.

## What's Editor-only (needs doing by hand)

### 1. Turn `Container_Optimal_M1` into the Canvas itself

No separate wrapper Canvas needed — Mission 5 splits `BridgeCanvas`
(screen-space UI) from `Container_Optimal_M5` (a world-space `Rigidbody2D`
physics playground) because those are two fundamentally different rendering
systems that can't share a hierarchy branch. The pipe puzzle has no such
mixing — pipes, background, everything is UI now — so there's no reason to
split them. Add `Canvas` (**Screen Space - Overlay**, matching
`BridgeCanvas`'s render mode), `CanvasScaler` (UI Scale Mode → Scale With
Screen Size, matching whatever the rest of the project's Canvases use), and
it'll get a `GraphicRaycaster` automatically — **directly on the existing
`Container_Optimal_M1` GameObject**, not a new object it gets moved under.
It's already separate from the `Mission1` world group, same as
`BridgeCanvas` is separate from `Mission5`.

**Remove the `CameraFollower` component from `Container_Optimal_M1`** — it's
dead weight now; a Screen Space - Overlay Canvas is inherently screen-fixed,
it doesn't need to re-center on the camera every frame the way a world-space
container did.

`Container_Optimal_M1` keeps starting inactive exactly as before —
`MinigameActivator.container.SetActive(true/false)` toggles this same object
directly, unchanged. (If you'd rather keep a separate Canvas wrapper anyway,
that still works — Unity auto-converts a reparented object's `Transform` to
`RectTransform` on becoming a Canvas child — but just remember a wrapper
Canvas has to stay permanently active itself in that case, since nothing in
code would ever activate it otherwise. Simplest is skipping the wrapper
entirely, as above.)

`Container_Optimal_M1` currently has two children: `Pipes` (the grid) and
`Setting menu_1` (the popup background). Both keep their same roles, just
now as UI elements — see below.

### 2. Convert the 4 pipe prefabs (`PipeCorner`/`PipeStraight`/`PipeTShape`/`PipeCross`)

Editing these prefab **assets** (not the individual scene instances)
propagates to every placed pipe automatically, since every pipe in the grid
is an instance of one of these four. For each of the 4 prefabs:

1. Open the prefab in Prefab Mode.
2. Note the currently-assigned sprite on `SpriteRenderer.m_Sprite` (the
   "empty"/unpowered look) before removing it.
3. **Remove** the `SpriteRenderer` component.
4. **Remove** the `BoxCollider2D` component — hit-testing is now
   `IPointerClickHandler` through the Canvas raycaster, not
   `Physics2D`/`Collider2D` at all.
5. **Add** an `Image` component. Unity requires `Image` to sit on a
   `RectTransform`, so adding it auto-upgrades the prefab root's `Transform`
   the same way reparenting under a Canvas does.
6. Set the new `Image.Source Image` to the same sprite the old
   `SpriteRenderer` had (the unpowered look) — this is what `PipeVisual.Awake`
   caches as `emptySprite`.
7. Leave **Raycast Target** checked (the `Image` default) — that's what makes
   `OnPointerClick` actually fire for this pipe.
8. Double check `PipeVisual`'s own fields survived untouched:
   `shapeType`/`gridX`/`gridY`/`filledSprite`/`rotateClip` are all on the
   `PipeVisual` component itself, not `SpriteRenderer`, so removing/adding
   other components shouldn't have touched them — but confirm anyway, since
   `filledSprite` (the powered look) is easy to forget to re-verify after a
   component swap. **This is exactly the calibration data
   `CLAUDE.md`'s Mission 1 section describes tuning per-shape — don't
   re-derive it, just carry the existing values over.**
9. Size the `RectTransform` (`Width`/`Height` in the Inspector) to match
   however big you want one grid cell to read on screen — there's no more
   "1 world unit = 1 sprite" assumption once this is UI-space, so pick a
   pixel size (e.g. `100 x 100`) and keep it consistent across all 4 prefabs
   so the grid tiles edge-to-edge.

### 3. Re-lay-out the grid

The old world-space grid positions (`Transform.localPosition`, in world
units) don't carry over to `RectTransform.anchoredPosition` (UI units)
automatically — each placed pipe instance needs repositioning. Simplest
approach: for each pipe, set
`anchoredPosition = (gridX * cellSize, gridY * cellSize)` using whatever
`cellSize` you picked in step 2.9 above (offset the whole grid however you
like to center it in the panel — the absolute origin doesn't matter, only
that it's consistent across all placed pipes, since `PipePuzzleSystem` only
ever reads `gridX`/`gridY`, never world/anchored position). This is more
robust than a `Grid Layout Group`'s sibling-order auto-fill, since not every
one of the 5×5 cells is populated (empty cells would break a naive
row-major-fill layout) — driving position directly off the already-authored
`gridX`/`gridY` keeps the visual grid tied to the same logical coordinates
`PipePuzzleSystem`'s flood-fill already uses, so it can't drift out of sync.

### 4. `Setting menu_1` background

Same "popup panel" background image, now as a UI `Image` (sliced or simple,
your call) sized to frame the whole pipe grid, placed behind it in sibling
order (Canvas draws later siblings on top, replacing the old
`SortingLayer`/`SortingOrder` concept `SpriteRenderer` used).

### 5. `PipePuzzleSystem`

No changes needed to the component itself — it can stay exactly where it
already is (on `Container_Optimal_M1`, alongside the pipes it manages). It
still finds every `PipeVisual` via
`FindObjectsByType<PipeVisual>(FindObjectsInactive.Include)`, which doesn't
care whether they're UI or world-space.

## Sanity check before playtesting

- `Container_Optimal_M1` starts inactive (unchanged requirement).
- No `Collider2D` remains anywhere under `Container_Optimal_M1` — if a click
  isn't rotating a pipe, the most likely cause is a leftover
  `BoxCollider2D` from before the migration still consuming the click before
  it reaches the `Image`'s raycast target, or **Raycast Target** unchecked on
  the `Image`.
- `Container_Optimal_M1`'s `GraphicRaycaster` is present and enabled (added
  automatically with the Canvas, but worth a glance if clicks don't land).
