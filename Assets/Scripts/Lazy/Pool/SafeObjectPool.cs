using System;
using Lazy.Pool.Factory;
using Lazy.Singleton;

namespace Lazy.Pool
{
    public class SafeObjectPool<T> : Pool<T>, ISingleton, IDisposable
        where T : IPoolable, new()
    {
        public static SafeObjectPool<T> Instance => SingletonProperty<SafeObjectPool<T>>.Instance;

        protected SafeObjectPool()
        {
            SetObjectFactory(new DefaultObjectFactory<T>());
        }

        public void OnSingletonInitialize()
        {
        }

        protected override void CustomFree(T obj)
        {
        }

        public void Dispose()
        {
            SingletonProperty<SafeObjectPool<T>>.Dispose();
        }
    }
}