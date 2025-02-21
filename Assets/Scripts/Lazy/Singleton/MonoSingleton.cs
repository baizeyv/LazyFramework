using System;
using UnityEngine;

namespace Lazy.Singleton
{
    public class MonoSingleton<T> : MonoBehaviour, ISingleton, IDisposable where T : MonoSingleton<T>
    {

        protected static T _instance;

        private static bool _applicationIsQuitting;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                    return null;
                if (_instance == null)
                {
                    _instance = SingletonCreator.CreateMonoSingleton<T>();
                }

                return _instance;
            }
        }

        public virtual void OnSingletonInitialize()
        {
        }

        public virtual void Dispose()
        {
            _instance = null;
            Destroy(gameObject);
        }

        protected virtual void OnApplicationQuit()
        {
            if (_instance == null)
                return;
            _applicationIsQuitting = true;
            Dispose();
        }
    }
}