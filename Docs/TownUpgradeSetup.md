# Town Upgrade & Rushed-Fix Breakdown — Editor Setup Guide

What changed in code (already done):

- **No more stages or days.** Missions can be played in any order. `StageManager`,
  `StageData`, `StageRegistry`, and the `Stage1`/`Stage2`/`StageRegistry` assets are
  gone.
- **Rushed fixes break down.** A mission solved trivially reopens (with redo
  hints) after the player completes their **next** mission. If it's the only
  unsolved mission left, it reopens right away. A notice panel announces it.
- **Gold Coins buy town upgrades at Town Hall:** 3 coins for the first, 2 more
  (all 5) for the final one. Every building upgrades at once. The final upgrade
  is the ending.
- **Town Hall is now just the upgrade shop.** There's no submission, and no
  "clear the streets" check.
- **Each upgrade slows trash spawning** (×2 interval after the first, ×4 after
  the final).
- **Inventory slot 0 is reserved for Gold Coins**, so trash can never cause a
  coin to be lost.
- **Renamed in place (existing scene components carry over automatically):**
  - `StageManager` → **`MissionReviewSystem`**
  - `DayCompleteUI` → **`TownNoticeUI`**
  - `TownHallUpgrade` → **`BuildingUpgrade`**
  - The `DayComplete` game state → **`TownNotice`**

Everything below is scene wiring that only the Editor can do. Open
`SampleScene`, let Unity finish compiling, and check the Console is free of
compile errors first.

## 1. MissionReviewSystem (was StageManager)

1. Select the **`StageManager`** GameObject. Its component is now
   **`MissionReviewSystem`**. Rename the GameObject to `MissionReviewSystem` to
   match.
2. Drag the **`MissionRegistry`** asset (`Assets/Data/Registry/`) into
   **Mission Registry**. The old Stage Registry field is gone.

## 2. Create the TownUpgradeSystem

1. Create an empty GameObject named **`TownUpgradeSystem`** (next to
   `MissionReviewSystem` in the Hierarchy).
2. Add the **`TownUpgradeSystem`** component.
3. Drag the **Gold Coin** `ItemData` into **Gold Coin Item**.
4. Leave **Upgrade Costs** at `[3, 2]`. The array length is the number of
   upgrades, so it must stay 2 to match 3 building tiers.

## 3. Reserve the coin slot

1. Select **`InventorySystem`**.
2. Drag the **Gold Coin** `ItemData` into **Reserved Slot Item**.
3. Optional: in `InventoryUI`, the first slot is now always the coin slot. A
   small coin icon or gold tint on its frame tells players why it stays empty
   until they earn a coin.

## 4. TownNoticeUI (was the Day Complete panel)

1. Select the old Day Complete panel. Its component is now **`TownNoticeUI`**.
   Rename the GameObject to `TownNoticePanel` to match.
2. Drag the **`MissionRegistry`** asset into **Mission Registry**. It's used to
   name the missions in breakdown notices.
3. **Re-pick the Dismiss button's OnClick.** Select the panel's dismiss Button.
   In **On Click ()**, set the function to **TownNoticeUI → OnDismiss** again.
   The saved entry still records the old type name `DayCompleteUI`, and it may
   not resolve after the rename. Re-selecting it removes the doubt.
4. Optional: edit **Upgrade Notices** (one title/body per upgrade; the second is
   the ending), **Breakdown Title**, and **Breakdown Body** (`{0}` = mission
   names). The defaults are already filled in.

## 5. Set up every building's tiers

Each upgradable building needs this shape:

```
Building_Blacksmith            ← BuildingUpgrade component here
 ├─ Tier0_Rundown              ← tiers[0]
 │   ├─ Base   (SpriteRenderer, EntityTilemap sorting layer)
 │   └─ Roof   (SpriteRenderer, ForeGroundTilemap sorting layer)
 ├─ Tier1_Improved             ← tiers[1]
 │   ├─ Base
 │   └─ Roof
 └─ Tier2_WellBuilt            ← tiers[2]
     ├─ Base
     └─ Roof
```

For each building:

1. Put the three versions under one parent as above. Base and Roof use the
   same sorting layers as the existing Town Hall (Base on `EntityTilemap`, Roof
   on `ForeGroundTilemap`) so the player still walks behind roofs.
2. Add **`BuildingUpgrade`** to the parent.
3. Set **Tiers** size to **3** and drag in the three tier children in order.
   Every slot must be filled.
4. You don't need to set which tier is active in the Editor. The script shows
   the correct tier on Play. Leaving Tier 0 active is still nicest for editing.

**Town Hall specifically:** its `BuildingUpgrade` has only **2** entries
carried over from the old script. Add the third tier object and set the size to
3, otherwise Play throws an error on the final upgrade.

**Collision:** walkability comes from the collision Tilemap, not from these
sprites. If a building's footprint differs between tiers, make the collision
tiles cover the largest footprint, or the player could walk through part of an
upgraded building.

## 6. Town Hall dialogue (replace the old text)

Select `TownHall` → `TownHallInteractable`:

- **Not Enough Coins Lines** loads with the *old* stage-era text ("Bring two
  gold coins before I can close out this stage"), because a field with that
  name already existed. Replace it. `{0}` is replaced with the number of coins
  still needed, for example:
  - `Welcome! The town's improvement fund is open.`
  - `Bring {0} more gold coin(s) - earned by fixing problems at their root - and we'll put them to work.`
- **Village Complete Lines** is new and pre-filled. It's shown after the final
  upgrade.
- The old **Incomplete Stage**, **All Stages Complete**, and **Trash On Ground**
  lines are gone. Unity drops their data the next time you save the scene.

## 7. Trash spawner and audio

- **`TrashManager`** → `TrashSpawner`: **Interval Multiplier Per Town Level**
  should read `[1, 2, 4]`. It needs exactly **3** entries, one per town tier.
- **`AudioManager`**: the old Day Completed clip carries over into **Town
  Upgraded Clip** automatically. Swap it if you want a different sound.

## 8. Check for missing references

Press Play once and check the **Console** for any "is not assigned!" errors.
Each one names the object that still needs a reference.

## 9. Update the Info Board text

The Info Board pages are saved in the scene, so the new defaults in code don't
reach your existing board. Select the Info Board's `InfoBoardUI` and update:

**Gold Coins & Trust** (body)
> Solve a mission's true root cause and you'll earn a Gold Coin - it always has its own inventory slot, so litter can never crowd it out. Spend coins at Town Hall to upgrade the whole village: 3 coins for the first upgrade, 2 more for the final one. Villagers also remember how you helped them: a trivial fix costs you their trust, while finding the real root cause earns it back.

**Trash & Your Inventory** (body)
> Rubbish appears randomly around town. Click a piece to pick it up - it'll take a slot in your inventory until you drop it off at the Trash Collection Site. Let it pile up and you'll run out of room for mission items. A better-kept town makes less litter, so each Town Hall upgrade slows it down.

**Town Hall & New Days**: change the title to **Quick Fixes & Town Hall**, body:
> Tackle missions in any order. A quick fix might look fine at first, but it won't last: after you finish your next mission, it breaks down and you'll need to take another look - with hints this time. Bring Gold Coins to Town Hall to upgrade the village. Upgrade it all the way and the village is complete.

**What You'll Find Around Town**: replace the last line with:
> - Town Hall: spend Gold Coins to upgrade the village.

## 10. Playtest checklist

- [ ] On Play, every building shows its **rundown** tier.
- [ ] Visit Town Hall with no coins: "Bring 3 more gold coin(s)…".
- [ ] **Any order:** start with an advanced mission (3, 4, or 5). It plays
      normally.
- [ ] **Breakdown:** solve mission A trivially. Nothing reopens yet. Solve any
      other mission B. After closing B's reflection, a "A Quick Fix Gave Way"
      notice names A, and A can be started again (with hints in the 5 Whys).
- [ ] **Last mission:** with every other mission optimal, solve the last one
      trivially. After closing the reflection, it breaks down immediately.
- [ ] Finish a mission optimally with the inventory full of trash: the coin
      still appears, in the **first** slot. Trash never goes into that slot.
- [ ] Trash on the ground never blocks anything at Town Hall.
- [ ] With 3 coins, visit Town Hall: upgrade notice, **all** buildings switch to
      the improved tier, 3 coins are removed, trash spawns noticeably less often.
- [ ] With coins 4 and 5, visit Town Hall: final notice (the ending), all
      buildings switch to the well-built tier. Visiting again shows the
      village-complete lines.
