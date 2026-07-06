public class ReflectionState : IState
{
    public void Enter() { }

    public void Tick()
    {
        if (PointerInput.PrimaryPressedThisFrame())
            ReflectionPopupUI.Instance.OnDismiss();
    }

    public void Exit() { }
}