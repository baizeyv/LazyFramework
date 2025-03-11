using Lazy.Singleton;

namespace Lazy.Manage
{
    public interface IManager
    {
        void OnUpdate();

        void OnFixedUpdate();

        void OnLateUpdate();

        void OnDestroy();
    }
}
