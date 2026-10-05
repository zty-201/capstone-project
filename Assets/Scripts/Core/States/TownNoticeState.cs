// Empty stub — the town notice panel is dismissed via a UI Button wired directly to
// TownNoticeUI.OnDismiss() in the Inspector, not through Tick().
public class TownNoticeState : IState
{
    public void Enter() { }
    public void Tick() { }
    public void Exit() { }
}
