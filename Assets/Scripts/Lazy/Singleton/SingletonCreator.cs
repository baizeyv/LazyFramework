using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Singleton
{
    internal static class SingletonCreator
    {
        internal static T CreateNonPublicConstructorObject<T>()
            where T : class
        {
            var type = typeof(T);
            var constructorInfos = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

            var ctor = Array.Find(constructorInfos, c => c.GetParameters().Length == 0);

            if (ctor == null)
            {
                throw new Exception("Non-Public Constructor() not found! in " + type);
            }

            return ctor.Invoke(null) as T;
        }

        public static T CreateSingleton<T>()
            where T : class, ISingleton
        {
            var instance = CreateNonPublicConstructorObject<T>();
            instance.OnSingletonInitialize();
            return instance;
        }

        public static T CreateMonoSingleton<T>() where T : class, ISingleton
        {
            T instance = null;
            var type = typeof(T);
            instance = Object.FindObjectOfType(type) as T;
            if (instance != null)
            {
                instance.OnSingletonInitialize();
                return instance;
            }

            var attributes = type.GetCustomAttributes(true);
            foreach (var attribute in attributes)
            {
                var defineAttribute = attribute as MonoSingletonPathAttribute;
                if (defineAttribute == null)
                    continue;
                instance = CreateComponentOnGameObject<T>(defineAttribute.PathInHierarchy, true);
                break;
            }

            if (instance == null)
            {
                var obj = new GameObject(type.Name);
                Object.DontDestroyOnLoad(obj);
                instance = obj.AddComponent(type) as T;
            }

            instance?.OnSingletonInitialize();
            return instance;
        }

        private static T CreateComponentOnGameObject<T>(string path, bool dontDestroy) where T : class
        {
            var obj = FindGameObject(path, dontDestroy);
            if (obj == null)
            {
                obj = new GameObject($"[Singleton {typeof(T).Name}]");
                if (dontDestroy)
                {
                    Object.DontDestroyOnLoad(obj);
                }
            }

            return obj.AddComponent(typeof(T)) as T;
        }

        private static GameObject FindGameObject(string path, bool dontDestroy)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            var subPath = path.Split('/');
            if (subPath == null || subPath.Length == 0)
                return null;

            return FindGameObject(null, subPath, 0, dontDestroy);
        }

        private static GameObject FindGameObject(GameObject root, string[] subPath, int index, bool dontDestroy)
        {
            GameObject obj = null;
            if (root == null)
            {
                obj = GameObject.Find(subPath[index]);
            }
            else
            {
                var child = root.transform.Find(subPath[index]);
                if (child != null)
                {
                    obj = child.gameObject;
                }
            }

            if (obj == null)
            {
                obj = new GameObject(subPath[index]);
                if (root != null)
                {
                    obj.transform.SetParent(root.transform);
                }
                if (dontDestroy && index == 0)
                    Object.DontDestroyOnLoad(obj);
            }

            if (obj == null)
                return null;
            return ++index == subPath.Length ? obj : FindGameObject(obj, subPath, index, dontDestroy);
        }
    }
}