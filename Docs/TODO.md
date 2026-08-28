# To Do

## Mission Directory HUD (top-left objective tracker)

**Scripting is implemented** — see `CLAUDE.md`'s "Mission Directory HUD" section for the full
mechanism (`EventBus.OnObjectiveProgress`, `MissionData.introObjective`/`trivialObjectives`/
`optimalObjectives`, `MissionDirectoryUI`). What's left is Editor-side scene wiring, since there's
no CLI build/test path for this project — all of it happens in the Unity Editor:

1. Add a top-left Canvas panel with one child `TextMeshProUGUI` per mission (a vertical layout
   group keeps the stack compact as lines disappear), matching the `MissionEntryUI`-per-mission
   pattern `MissionBoardUI` already uses.
2. Add a `MissionDirectoryUI` component, assign its `entries` array — one `DirectoryEntry` per
   mission, each pointing at the matching `MissionData` asset and its `TextMeshProUGUI` line.
3. Play-test both missions' trivial and optimal paths, plus a Stage Gate reopen-for-review redo,
   to confirm the line text advances and resets as expected.

The two `MissionData` assets (`M1_ParchedCrops`, `M2_CleaningRiver`) already have their
`introObjective`/`trivialObjectives`/`optimalObjectives` fields populated — no data authoring left
there, just wiring the display.
