public enum GameStateType
{
    Exploration,
    Dialogue,
    Planning,
    Puzzle,
    BridgeBuilder,
    Reflection,
    MissionBoard,
    DayComplete,
    InfoBoard,
    SettingsMenu,
    // Appended, not inserted: MinigameActivator.targetState serializes this enum by its raw int
    // value in the scene, so adding a member in the middle would silently shift every later
    // member's value out from under already-authored Inspector data (e.g. Mission 5's
    // targetState: 4 for BridgeBuilder). Appending is the only order-safe way to add one.
    RoutineBuilder
}
