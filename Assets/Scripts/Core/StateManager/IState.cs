public interface IState
{
    // Called once when the state begins
    void Enter();

    // Called every frame while this state is active
    void Execute();

    // Called once just before the state switches to another one
    void Exit();
}