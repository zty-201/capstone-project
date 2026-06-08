public interface IState
{
    void Enter(); // Called once when transitioning into the state
    void Tick();  // Called every frame (your Update loop)
    void Exit();  // Called once when transitioning out of the state
}