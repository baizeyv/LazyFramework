using Lazy;

namespace Lazy
{
    public interface IManager
    {
        void OnUpdate();

        void OnFixedUpdate();

        void OnLateUpdate();

        void OnDestroyRelease();

        void OnGui();
    }
}
