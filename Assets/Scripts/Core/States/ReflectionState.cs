using UnityEngine.InputSystem;

public class ReflectionState : IState
{
    public void Enter() { }

    public void Tick()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            ReflectionPopupUI.Instance.OnDismiss();
    }

    public void Exit() { }
}