namespace Lazy
{
    public interface IState
    {
        bool Condition();

        void Enter();

        void Update();

        void FixedUpdate();

        void GUI();

        void Exit();
    }
}
