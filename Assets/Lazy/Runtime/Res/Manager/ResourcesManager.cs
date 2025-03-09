using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lazy.Res.Loader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Res.Manager
{
    public class ResourcesManager
    {
        private readonly Dictionary<string, ResourcesLoader> _resourceLoaders = new();

        /// <summary>
        /// * 同步加载Resources资源
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T LoadSync<T>(string resourcePath)
            where T : Object
        {
            if (_resourceLoaders.TryGetValue(resourcePath, out var loader))
                return loader.LoadSync<T>();

            loader = LoaderFactory.CreateLoader(resourcePath);
            _resourceLoaders.Add(resourcePath, loader);

            return loader.LoadSync<T>();
        }

        /// <summary>
        /// * 同步加载Resources资源
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public Object LoadSync(string resourcePath, Type resourceType)
        {
            if (_resourceLoaders.TryGetValue(resourcePath, out var loader))
                return loader.LoadSync(resourceType);

            loader = LoaderFactory.CreateLoader(resourcePath);
            _resourceLoaders.Add(resourcePath, loader);

            return loader.LoadSync(resourceType);
        }

        /// <summary>
        /// * 同步加载Resources资源
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public Object LoadSync(string resourcePath)
        {
            if (_resourceLoaders.TryGetValue(resourcePath, out var loader))
                return loader.LoadSync();

            loader = LoaderFactory.CreateLoader(resourcePath);
            _resourceLoaders.Add(resourcePath, loader);

            return loader.LoadSync();
        }

        /// <summary>
        /// * 异步加载Resources资源
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="callback"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ResourcesLoader LoadAsync<T>(
            string resourcePath,
            AssetLoadedCallback<T> callback = null
        )
            where T : Object
        {
            if (!_resourceLoaders.TryGetValue(resourcePath, out var loader))
            {
                loader = LoaderFactory.CreateLoader(resourcePath);
                _resourceLoaders.Add(resourcePath, loader);
            }

            loader.LoadAsync<T>(callback);
            return loader;
        }

        /// <summary>
        /// * 异步加载Resource资源
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="resourceType"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ResourcesLoader LoadAsync(
            string resourcePath,
            Type resourceType,
            AssetLoadedCallback<Object> callback = null
        )
        {
            if (!_resourceLoaders.TryGetValue(resourcePath, out var loader))
            {
                loader = LoaderFactory.CreateLoader(resourcePath);
                _resourceLoaders.Add(resourcePath, loader);
            }

            loader.LoadAsync(resourceType, callback);
            return loader;
        }

        /// <summary>
        /// * 异步加载Resource资源
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ResourcesLoader LoadAsync(
            string resourcePath,
            AssetLoadedCallback<Object> callback = null
        )
        {
            if (!_resourceLoaders.TryGetValue(resourcePath, out var loader))
            {
                loader = LoaderFactory.CreateLoader(resourcePath);
                _resourceLoaders.Add(resourcePath, loader);
            }

            loader.LoadAsync(callback);
            return loader;
        }

        /// <summary>
        /// * 协程加载Resource资源
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerator LoadCoroutine<T>(string resourcePath)
            where T : Object
        {
            if (!_resourceLoaders.TryGetValue(resourcePath, out var loader))
            {
                loader = LoaderFactory.CreateLoader(resourcePath);
                _resourceLoaders.Add(resourcePath, loader);
            }

            yield return loader.LoadCoroutine<T>();
        }

        /// <summary>
        /// * 协程加载Resource资源
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public IEnumerator LoadCoroutine(string resourcePath, Type resourceType = null)
        {
            if (!_resourceLoaders.TryGetValue(resourcePath, out var loader))
            {
                loader = LoaderFactory.CreateLoader(resourcePath);
                _resourceLoaders.Add(resourcePath, loader);
            }

            yield return loader.LoadCoroutine(resourceType);
        }

        /// <summary>
        /// * 加载文件夹下所有资源
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="subAssetName"></param>
        /// <param name="loader"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T LoadAll<T>(string resourcePath, string subAssetName, out ResourcesLoader loader)
            where T : Object
        {
            if (!_resourceLoaders.TryGetValue(resourcePath, out var myLoader))
            {
                myLoader = LoaderFactory.CreateLoader(resourcePath);
                _resourceLoaders.Add(resourcePath, myLoader);
            }

            loader = myLoader;
            return loader.LoadAll<T>(subAssetName);
        }

        /// <summary>
        /// * 加载文件夹下所有指定类型的资源
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="resourceType"></param>
        /// <param name="subAssetname"></param>
        /// <param name="loader"></param>
        /// <returns></returns>
        public Object LoadAll(
            string resourcePath,
            Type resourceType,
            string subAssetname,
            out ResourcesLoader loader
        )
        {
            if (!_resourceLoaders.TryGetValue(resourcePath, out var myLoader))
            {
                myLoader = LoaderFactory.CreateLoader(resourcePath);
                _resourceLoaders.Add(resourcePath, myLoader);
            }

            loader = myLoader;
            return loader.LoadAll(resourceType, subAssetname);
        }

        /// <summary>
        /// * 卸载指定资源
        /// </summary>
        /// <param name="resourcePath"></param>
        public void Unload(string resourcePath)
        {
            if (!_resourceLoaders.TryGetValue(resourcePath, out var loader))
                return;
            LoaderFactory.ReleaseLoader(loader);
            _resourceLoaders.Remove(resourcePath);
        }

        /// <summary>
        /// * 卸载指定资源加载器
        /// </summary>
        /// <param name="loader"></param>
        public void Unload(ResourcesLoader loader)
        {
            if (loader == null)
                return;
            if (_resourceLoaders.ContainsValue(loader))
            {
                var keys = (
                    from kv in _resourceLoaders
                    where kv.Value == loader
                    select kv.Key
                ).ToList();
                foreach (var key in keys)
                    Unload(key);
            }
            else
            {
                LoaderFactory.ReleaseLoader(loader);
            }
        }

        /// <summary>
        /// * 卸载指定资源
        /// </summary>
        /// <param name="obj"></param>
        public void Unload(Object obj)
        {
            if (obj == null)
                return;

            var keys = (from kv in _resourceLoaders where kv.Value.Is(obj) select kv.Key).ToList();
            foreach (var key in keys)
                Unload(key);

            if (keys.Count <= 0)
                Resources.UnloadAsset(obj);
        }

        /// <summary>
        /// * 获取当前所有加载器的加载进度
        /// </summary>
        /// <returns></returns>
        public float GetAllLoadProgress()
        {
            // # 加载器数量
            var loaderCount = _resourceLoaders.Values.Count;

            if (loaderCount == 0)
                return -1f;

            var progress = _resourceLoaders.Values.Sum(item => item.Progress);

            return progress / loaderCount;
        }

        /// <summary>
        /// * 获取指定资源的加载进度
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public float GetLoadProgress(string resourcePath)
        {
            if (_resourceLoaders.TryGetValue(resourcePath, out var loader))
                return loader.Progress;

            return -1;
        }

        /// <summary>
        /// * 查询当前所有加载器是否全部加载完成
        /// </summary>
        /// <returns></returns>
        public bool IsAllLoaded()
        {
            return _resourceLoaders.Values.All(item => item.IsLoaded);
        }

        /// <summary>
        /// * 查询指定资源是否加载完成
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public bool IsLoaded(string resourcePath)
        {
            return _resourceLoaders.TryGetValue(resourcePath, out var loader) && loader.IsLoaded;
        }

        /// <summary>
        /// * 获取指定资产对象
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="loader"></param>
        /// <param name="subAssetName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetAssetObject<T>(
            string resourcePath,
            out ResourcesLoader loader,
            string subAssetName = null
        )
            where T : Object
        {
            if (IsLoaded(resourcePath))
            {
                loader = _resourceLoaders[resourcePath];
                return loader.GetAssetObject<T>(subAssetName);
            }

            loader = null;
            return null;
        }

        /// <summary>
        /// * 获取指定资产对象
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="loader"></param>
        /// <param name="subAssetName"></param>
        /// <returns></returns>
        public Object GetAssetObject(
            string resourcePath,
            out ResourcesLoader loader,
            string subAssetName = null
        )
        {
            if (IsLoaded(resourcePath))
            {
                loader = _resourceLoaders[resourcePath];
                return loader.GetAssetObject(subAssetName);
            }

            loader = null;
            return null;
        }

        /// <summary>
        /// * 清空ResourcesManager
        /// </summary>
        private void Clear()
        {
            foreach (var loader in _resourceLoaders.Values)
                LoaderFactory.ReleaseLoader(loader);
            _resourceLoaders.Clear();
        }
    }
}
