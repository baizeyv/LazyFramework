using Lazy.Singleton;

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
