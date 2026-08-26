// "Act" is deliberately not a distinct visible phase — the indicator hides (None) when the
// player returns to Exploration after Reflection, representing "go apply what you learned"
// without inventing a 4th on-screen state with no dedicated screen to anchor it to.
public enum PDCAPhase { None, Plan, Do, Check }
