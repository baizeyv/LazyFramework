using System;
using System.Collections.Generic;
using System.Linq;

namespace Lazy.IOC
{
    /// <summary>
    /// * 控制反转容器
    /// </summary>
    public class IOCContainer
    {
        private Dictionary<Type, object> _instances = new();

        public void Register<T>(T instance)
        {
            var key = typeof(T);
            _instances[key] = instance;
        }

        public void Unregister<T>()
        {
            var key = typeof(T);
            _instances.Remove(key);
        }

        public void RegisterSpecial<T>(object obj)
        {
            var key = typeof(T);
            if (_instances.ContainsKey(key))
                _instances[key] = obj;
            else
                _instances.Add(key, obj);
        }

        public object GetSpecial<T>()
            where T : class
        {
            var key = typeof(T);
            return _instances.GetValueOrDefault(key);
        }

        public T Get<T>()
            where T : class
        {
            var key = typeof(T);
            if (_instances.TryGetValue(key, out var retInstance))
                return retInstance as T;

            return null;
        }

        public bool TryGet<T>(out T instance)
            where T : class
        {
            var key = typeof(T);
            if (_instances.TryGetValue(key, out var retInstance))
            {
                instance = retInstance as T;
                return true;
            }

            instance = null;
            return false;
        }

        public IEnumerable<T> GetInstancesByType<T>()
        {
            var type = typeof(T);
            return _instances.Values.Where(item => type.IsInstanceOfType(item)).Cast<T>();
        }

        public Dictionary<Type, object> GetMap()
        {
            return _instances;
        }

        public void Clear()
        {
            _instances.Clear();
        }
    }
}
