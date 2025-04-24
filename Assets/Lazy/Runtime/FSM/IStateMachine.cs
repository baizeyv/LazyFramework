using Lazy;

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
