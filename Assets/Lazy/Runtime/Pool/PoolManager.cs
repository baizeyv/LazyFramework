using Lazy.Manage;
using Lazy.Pool.GameObject;
using Lazy.Ref;
using Lazy.Singleton;

namespace Lazy.Pool
{
    public class PoolManager : Singleton<PoolManager>, IManager
    {
        public GameObjectPoolManager GameObjectPool { get; private set; }

        private PoolManager() { }

        public override void OnSingletonInitialize()
        {
            GameObjectPool = ManagerCenter.Create(() => GameObjectPoolManager.Instance);
            ManagerCenter.CreateMono(() => GlobalPoolInstaller.Instance);
        }

        #region API

        public T ObtainSafe<T>()
            where T : IPoolable, new()
        {
            return SafeObjectPool<T>.Instance.Obtain();
        }

        public void FreeSafe<T>(T obj)
            where T : IPoolable, new()
        {
            SafeObjectPool<T>.Instance.Free(obj);
        }

        public T ObtainRef<T>()
            where T : class, IReference, new()
        {
            return ReferencePool.Instance.Obtain<T>();
        }

        public void FreeRef<T>(T obj)
            where T : class, IReference, new()
        {
            ReferencePool.Instance.Free(obj);
        }

        #endregion


        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroy() { }
    }
}
