using Lazy.Pool;

namespace Lazy.FSM
{
    public interface IStateMachine
    {
        void Update();
        void FixedUpdate();
        void GUI();
        void End();
    }
}
