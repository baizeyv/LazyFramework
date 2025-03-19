using Lazy.Manage;
using Lazy.Singleton;

namespace Lazy.Debugger
{
    [MonoSingletonPath("Lazy/Debugger")]
    public class Debugger : MonoSingleton<Debugger>, IManager
    {
        private Debugger() { }

        public override void OnSingletonInitialize()
        {
            // TODO:
        }

        // TODO:
        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroy() { }
    }
}
