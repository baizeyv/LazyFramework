using Lazy.Pool;

namespace Lazy
{
    public interface IStateMachine
    {
        void Update();
        void FixedUpdate();
        void GUI();
        void End();
    }
}
