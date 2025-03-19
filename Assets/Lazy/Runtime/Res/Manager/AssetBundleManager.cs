using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lazy.Manage;
using Lazy.Res.Loader;
using Lazy.Singleton;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Res.Manager
{
    [ManagerUpdate]
    public class AssetBundleManager : Singleton<AssetBundleManager>, IManager
    {
        private AssetBundleManifest _manifest;

        /// <summary>
        /// * 加载器字典 Key:资源
        /// </summary>
        private Dictionary<string, AssetBundleLoader> _assetBundleLoaders = new();

        private AssetBundleManager() { }

        /// <summary>
        /// * 同步加载AssetBundle包
        /// </summary>
        /// <param name="assetType"></param>
        /// <param name="info"></param>
        /// <param name="subAssetName"></param>
        /// <returns></returns>
        public AssetBundle LoadSync(Type assetType, ref AssetInfo info, string subAssetName = null)
        {
            List<string> assetBundlePaths = new(GetDependenciesAssetBundles(info.AssetBundleName));

            for (var i = 0; i < assetBundlePaths.Count; i++)
                assetBundlePaths[i] = GetAssetBundlePathByAssetBundleName(assetBundlePaths[i]);
            assetBundlePaths.Add(info.AssetBundlePath);

            var loadedCount = 0;
            foreach (var assetBundlePath in assetBundlePaths)
            {
                if (_assetBundleLoaders.TryGetValue(assetBundlePath, out var loader))
                {
                    loader.AddParentBundle(info.AssetBundlePath);
                }
                else
                {
                    loader = LoaderFactory.CreateABLoader(assetBundlePath);
                    loader.AddParentBundle(info.AssetBundlePath);
                    _assetBundleLoaders.Add(assetBundlePath, loader);
                }

                // # 同步清理异步
                if (loader.AssetBundleLoadRequest?.assetBundle)
                    loader.AssetBundleLoadRequest?.assetBundle.Unload(false);

                loader.LoadSync();
                ++loadedCount;

                if (loadedCount == assetBundlePaths.Count)
                    loader.ExpandSync(info.AssetPath[0], assetType, subAssetName);
            }

            var result = GetAssetBundle(info.AssetBundlePath);
            return result;
        }

        /// <summary>
        /// * 异步加载AssetBundle
        /// </summary>
        /// <param name="assetType"></param>
        /// <param name="info"></param>
        /// <param name="subAssetName"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public AssetBundleLoader LoadAsync(
            Type assetType,
            AssetInfo info,
            string subAssetName = null,
            AssetBundleLoader.OnLoadFinished callback = null
        )
        {
            var assetBundlePaths = new List<string>(
                GetDependenciesAssetBundles(info.AssetBundleName)
            );

            for (var i = 0; i < _assetBundleLoaders.Count; i++)
                assetBundlePaths[i] = GetAssetBundlePathByAssetBundleName(assetBundlePaths[i]);
            assetBundlePaths.Add(info.AssetBundlePath);

            var loadedCount = 0;
            var endIndex = assetBundlePaths.Count - 1;
            AssetBundleLoader lastLoader = null;
            for (var i = endIndex; i >= 0; i--)
            {
                var assetBundlePath = assetBundlePaths[i];
                if (_assetBundleLoaders.TryGetValue(assetBundlePath, out var loader))
                {
                    loader.AddParentBundle(info.AssetBundlePath);
                }
                else
                {
                    loader = LoaderFactory.CreateABLoader(assetBundlePath);
                    loader.AddParentBundle(info.AssetBundlePath);
                    _assetBundleLoaders.Add(assetBundlePath, loader);
                }

                if (lastLoader == null)
                {
                    lastLoader = loader; // # 获取最后一个Loader
                    foreach (var t in assetBundlePaths)
                        loader.AddDependent(t);
                }

                var tmpLoader = lastLoader;
                loader.LoadAsync(_ =>
                {
                    ++loadedCount;
                    tmpLoader.AddDependent(assetBundlePath, true);
                    if (loadedCount == assetBundlePaths.Count)
                        // # 所有依赖项加载完成后，加载主资源
                        tmpLoader.ExpandAsync(
                            info.AssetPath[0],
                            assetType,
                            subAssetName,
                            () =>
                            {
                                // # 主资源加载完成后，如果需要展开，则在展开完成后回调
                                callback?.Invoke(GetAssetBundle(info.AssetBundlePath));
                            }
                        );
                });
            }

            return lastLoader;
        }

        /// <summary>
        /// * 协程加载AssetBundle
        /// </summary>
        /// <param name="assetType"></param>
        /// <param name="info"></param>
        /// <param name="subAssetName"></param>
        /// <returns></returns>
        public IEnumerator LoadCoroutine(Type assetType, AssetInfo info, string subAssetName = null)
        {
            var assetBundlePaths = new List<string>(
                GetDependenciesAssetBundles(info.AssetBundleName)
            );

            for (var i = 0; i < _assetBundleLoaders.Count; i++)
                assetBundlePaths[i] = GetAssetBundlePathByAssetBundleName(assetBundlePaths[i]);
            assetBundlePaths.Add(info.AssetBundlePath);

            var endIndex = assetBundlePaths.Count - 1;
            AssetBundleLoader lastLoader = null;
            for (var i = endIndex; i >= 0; i--)
            {
                var assetBundlePath = assetBundlePaths[i];
                if (_assetBundleLoaders.TryGetValue(assetBundlePath, out var loader))
                {
                    loader.AddParentBundle(info.AssetBundlePath);
                }
                else
                {
                    loader = LoaderFactory.CreateABLoader(assetBundlePath);
                    loader.AddParentBundle(info.AssetBundlePath);
                    _assetBundleLoaders.Add(assetBundlePath, loader);
                }

                if (lastLoader == null)
                {
                    lastLoader = loader; // # 获取最后一个Loader
                    foreach (var t in assetBundlePaths)
                        loader.AddDependent(t);
                }

                var tmpLoader = lastLoader;
                loader.LoadAsync(_ =>
                {
                    tmpLoader.AddDependent(assetBundlePath, true);
                });
            }

            yield return new WaitUntil(() =>
            {
                var count = 0;
                foreach (var t in assetBundlePaths)
                {
                    if (!_assetBundleLoaders.TryGetValue(t, out var value))
                        continue;
                    if (value.IsLoaded)
                        count++;
                }

                return count > endIndex;
            });

            yield return lastLoader!.ExpandCoroutine(info.AssetPath[0], assetType, subAssetName);
        }

        /// <summary>
        /// * 同步卸载AssetBundle
        /// </summary>
        /// <param name="assetBundlePath"></param>
        /// <param name="unloadAllLoadedObjects"></param>
        public void UnloadSync(string assetBundlePath, bool unloadAllLoadedObjects = false)
        {
            var bundleLoaders = GetRelatedLoaders(assetBundlePath);
            foreach (var loader in bundleLoaders)
            {
                if (unloadAllLoadedObjects)
                {
                    loader.RemoveParentBundle(assetBundlePath);
                    loader.RemoveDependent(assetBundlePath);
                }

                loader.UnloadSync(unloadAllLoadedObjects);
            }
        }

        /// <summary>
        /// * 同步卸载
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="unloadAllLoadedObjects"></param>
        public void UnloadSync(AssetBundleLoader loader, bool unloadAllLoadedObjects = false)
        {
            if (loader == null)
                return;
            if (_assetBundleLoaders.ContainsValue(loader))
            {
                var keys = (
                    from kv in _assetBundleLoaders
                    where kv.Value == loader
                    select kv.Key
                ).ToList();

                foreach (var key in keys)
                    UnloadSync(key, unloadAllLoadedObjects);
            }
            else
            {
                loader.Clear(unloadAllLoadedObjects);
            }
        }

        public void UnloadSync(AssetBundle bundle, bool unloadAllLoadedObjects = false)
        {
            if (bundle == null)
                return;

            var keys = (
                from kv in _assetBundleLoaders
                where kv.Value.AssetBundle == bundle
                select kv.Key
            ).ToList();

            foreach (var key in keys)
                UnloadSync(key, unloadAllLoadedObjects);

            if (keys.Count == 0)
                bundle.Unload(unloadAllLoadedObjects);
        }

        public void UnloadAsync(
            string assetBundlePath,
            bool unloadAllLoadedObjects = false,
            AssetBundleLoader.OnUnloadFinished callback = null
        )
        {
            var bundleLoaders = GetRelatedLoaders(assetBundlePath);
            var unloadedCount = 0;

            foreach (var loader in bundleLoaders)
                loader.UnloadAsync(
                    unloadAllLoadedObjects,
                    () =>
                    {
                        ++unloadedCount;
                        if (unloadedCount != bundleLoaders.Count)
                            return;
                        foreach (var l in bundleLoaders.Where(_ => unloadAllLoadedObjects))
                        {
                            l.RemoveParentBundle(assetBundlePath);
                            l.RemoveDependent(assetBundlePath);
                        }

                        callback?.Invoke();
                    }
                );
        }

        public void UnloadAsync(
            AssetBundleLoader loader,
            bool unloadAllLoadedObjects = false,
            AssetBundleLoader.OnUnloadFinished callback = null
        )
        {
            if (loader == null)
                return;

            if (_assetBundleLoaders.ContainsValue(loader))
            {
                var keys = (
                    from kv in _assetBundleLoaders
                    where kv.Value == loader
                    select kv.Key
                ).ToList();

                foreach (var key in keys)
                    UnloadAsync(key, unloadAllLoadedObjects, callback);
            }
            else
            {
                loader.UnloadAsync(unloadAllLoadedObjects, callback);
            }
        }

        public void UnloadAsync(
            AssetBundle bundle,
            bool unloadAllLoadedObjects = false,
            AssetBundleLoader.OnUnloadFinished callback = null
        )
        {
            if (bundle == null)
                return;

            var keys = (
                from kv in _assetBundleLoaders
                where kv.Value.AssetBundle == bundle
                select kv.Key
            ).ToList();

            foreach (var key in keys)
                UnloadAsync(key, unloadAllLoadedObjects, callback);

            if (keys.Count > 0)
                return;
            var op = bundle.UnloadAsync(unloadAllLoadedObjects);
            if (op != null && callback != null)
                op.completed += _ => callback();
        }

        public T GetAssetObject<T>(
            string assetBundlePath,
            string assetPath,
            string subAssetName,
            out AssetBundleLoader loader
        )
            where T : Object
        {
            if (_assetBundleLoaders.TryGetValue(assetBundlePath, out var loader2))
                if (loader2 is { IsLoaded: true, IsExpandCompleted: true })
                {
                    loader = loader2;
                    var success = loader2.TryGetAsset(
                        string.IsNullOrEmpty(subAssetName) ? assetPath : assetPath + subAssetName,
                        out var obj
                    );
                    if (success)
                        return obj as T;
                }

            loader = null;
            return null;
        }

        /// <summary>
        /// * 获取资产对象
        /// </summary>
        /// <param name="assetBundlePath">AssetBundle路径</param>
        /// <param name="assetPath">assetPath名(小写)</param>
        /// <param name="subAssetName">子资产名称</param>
        /// <param name="loader"></param>
        /// <returns></returns>
        public Object GetAssetObject(
            string assetBundlePath,
            string assetPath,
            string subAssetName,
            out AssetBundleLoader loader
        )
        {
            if (_assetBundleLoaders.TryGetValue(assetBundlePath, out var loader2))
                if (loader2 != null && loader2.IsLoaded && loader2.IsExpandCompleted)
                {
                    loader = loader2;
                    var success = loader2.TryGetAsset(
                        string.IsNullOrEmpty(subAssetName) ? assetPath : assetPath + subAssetName,
                        out var obj
                    );
                    if (success)
                        return obj;
                }

            loader = null;
            return null;
        }

        /// <summary>
        /// * 获取加载器
        /// </summary>
        /// <param name="assetBundlePath"></param>
        /// <returns></returns>
        public AssetBundleLoader GetAssetBundleLoader(string assetBundlePath)
        {
            return _assetBundleLoaders.TryGetValue(assetBundlePath, out var loader) ? loader : null;
        }

        /// <summary>
        /// * 通过资源包路径获取已加载的资源包
        /// </summary>
        /// <param name="assetBundlePath"></param>
        /// <returns></returns>
        public AssetBundle GetAssetBundle(string assetBundlePath)
        {
            if (!IsLoaded(assetBundlePath))
                return null;
            return _assetBundleLoaders.TryGetValue(assetBundlePath, out var loader)
                ? loader.AssetBundle
                : null;
        }

        public Hash128 GetAssetBundleHash(string assetBundleName)
        {
            return _manifest == null ? default : _manifest.GetAssetBundleHash(assetBundleName);
        }

        private string[] GetDependenciesAssetBundles(string assetBundleName)
        {
            return _manifest == null
                ? new string[] { }
                : _manifest.GetAllDependencies(assetBundleName);
        }

        private string GetAssetBundlePathByAssetBundleName(string assetBundleName)
        {
            return AssetBundlePathHelper.GetAssetBundlePathByAssetBundleName(assetBundleName);
        }

        /// <summary>
        /// * 获取有关的AssetBundleLoader
        /// </summary>
        /// <param name="assetBundlePath"></param>
        /// <returns></returns>
        private List<AssetBundleLoader> GetRelatedLoaders(string assetBundlePath)
        {
            return _assetBundleLoaders
                .Values.Where(loader => loader.IsParentBundle(assetBundlePath))
                .ToList();
        }

        /// <summary>
        /// * 查询指定的资源包是否已加载完成
        /// </summary>
        /// <param name="assetBundlePath"></param>
        /// <returns></returns>
        public bool IsLoaded(string assetBundlePath)
        {
            var bundleLoaders = GetRelatedLoaders(assetBundlePath);
            return bundleLoaders.Count != 0 && bundleLoaders.All(loader => loader.IsLoaded);
        }

        /// <summary>
        /// * 获取所有加载器的加载进度
        /// </summary>
        /// <returns></returns>
        public float Progress =>
            _assetBundleLoaders.Values.Count <= 0
                ? -1f
                : _assetBundleLoaders.Values.Sum(loader => loader.Progress)
                    / _assetBundleLoaders.Values.Count;

        /// <summary>
        /// * 获取所有加载器的扩展进度
        /// </summary>
        public float ExpandProgress =>
            _assetBundleLoaders.Values.Count <= 0
                ? -1f
                : _assetBundleLoaders.Values.Sum(loader => loader.ExpandProgress)
                    / _assetBundleLoaders.Values.Count;

        /// <summary>
        /// * 获取所有加载器的卸载进度
        /// </summary>
        public float UnloadProgress
        {
            get
            {
                var cnt = _assetBundleLoaders.Values.Count;
                if (cnt == 0)
                    return -1f;

                var allProgress = 0f;
                var hasUnloadLoader = false;
                foreach (
                    var loader in _assetBundleLoaders.Values.Where(loader => loader.IsUnloading)
                )
                {
                    hasUnloadLoader = true;
                    allProgress += loader.UnloadProgress;
                }

                if (hasUnloadLoader)
                    return allProgress / cnt;

                return 1f;
            }
        }

        /// <summary>
        /// * 通过资源包路径获取加载器的加载进度
        /// </summary>
        /// <param name="assetBundlePath"></param>
        /// <returns></returns>
        public float GetProgress(string assetBundlePath)
        {
            var bundleLoaders = GetRelatedLoaders(assetBundlePath);
            if (bundleLoaders.Count == 0)
                return -1f;

            var allProgress = bundleLoaders.Sum(loader => loader.Progress);

            return allProgress / bundleLoaders.Count;
        }

        /// <summary>
        /// * 通过资源包路径获取加载器的扩展进度
        /// </summary>
        /// <param name="assetBundlePath"></param>
        /// <returns></returns>
        public float GetExpandProgress(string assetBundlePath)
        {
            var bundleLoaders = GetRelatedLoaders(assetBundlePath);
            if (bundleLoaders.Count == 0)
                return -1f;

            var allProgress = bundleLoaders.Sum(loader => loader.ExpandProgress);

            return allProgress / bundleLoaders.Count;
        }

        /// <summary>
        /// * 通过资源包路径获取加载器的卸载进度
        /// </summary>
        /// <param name="assetBundlePath"></param>
        /// <returns></returns>
        public float GetUnloadProgress(string assetBundlePath)
        {
            var bundleLoaders = GetRelatedLoaders(assetBundlePath);
            var cnt = bundleLoaders.Count;
            if (cnt == 0)
                return -1f;
            var allProgress = 0f;
            var hasUnloadLoader = false;
            foreach (var loader in bundleLoaders.Where(loader => loader.IsUnloading))
            {
                hasUnloadLoader = true;
                allProgress += loader.UnloadProgress;
            }

            if (hasUnloadLoader)
                return allProgress / cnt;

            return 1f;
        }

        /// <summary>
        /// * 查询所有资源包是否加载完成
        /// </summary>
        public bool IsLoadFinished
        {
            get { return _assetBundleLoaders.Values.All(loader => loader.IsLoaded); }
        }

        /// <summary>
        /// * 查询所有资产对象是否已加载完成。
        /// </summary>
        public bool IsExpandFinished
        {
            get { return _assetBundleLoaders.Values.All(loader => loader.IsExpandCompleted); }
        }

        /// <summary>
        /// * 查询所有资源包是否已卸载完成。
        /// </summary>
        public bool IsUnloadFinished
        {
            get { return _assetBundleLoaders.Values.All(loader => loader.IsUnloaded); }
        }

        public void Clear()
        {
            foreach (var loader in _assetBundleLoaders.Values)
                loader.Clear(true);
            _assetBundleLoaders.Clear();
        }

        /// <summary>
        /// ! WebGL 专用异步加载AssetBundleManifest
        /// </summary>
        /// <returns></returns>
        public IEnumerator LoadAssetBundleManifest()
        {
            var manifestPath = AssetBundlePathHelper.GetAssetBundlePathByAssetBundleName(
                PathSetting.GetPlatformName()
            );
            if (manifestPath == null)
                yield break;
#if UNITY_EDITOR
            manifestPath = Path.IsPathRooted(manifestPath)
                ? "file://" + manifestPath
                : manifestPath;
#endif
            var request = new DownloadRequest(manifestPath, default);
            yield return request.SendAssetBundleDownloadRequestCoroutine(manifestPath);
            if (request.DownloadedAssetBundle)
            {
                _manifest = request.DownloadedAssetBundle.LoadAsset<AssetBundleManifest>(
                    "AssetBundleManifest"
                );
                _manifest.GetAllAssetBundles();
                request.DownloadedAssetBundle.Unload(false);
            }
            else
            {
                Log.Log.MsgE("如果游戏中没有使用任何AB包加载资源，可以删除此方法的调用！");
            }
        }

        public override void OnSingletonInitialize()
        {
#if UNITY_WEBGL
            Log.Log.MsgW(
                "（提示）由于WebGL需要异步加载AssetBundleManifest，已在资产模块之后加上：yield return AssetBundleManager.Instance.LoadAssetBundleManifest();"
            );
#else
            var manifestPath = AssetBundlePathHelper.GetAssetBundlePathByAssetBundleName(
                PathSetting.GetPlatformName()
            );
            if (string.IsNullOrEmpty(manifestPath))
                return;
            var assetBundle = AssetBundle.LoadFromFile(manifestPath);
            if (assetBundle)
            {
                _manifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                _manifest.GetAllAssetBundles();
                assetBundle.Unload(false);
            }
            else
            {
                Log.Log.MsgE("如果游戏中没有使用任何AB包加载资源，可以删除此方法的调用！");
            }
#endif
        }

        public void OnUpdate()
        {
            var assetBundleLoadersList = _assetBundleLoaders
                .Values.Where(loader => loader != null)
                .ToList();
            foreach (var t in assetBundleLoadersList)
                t.OnUpdate();
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroyRelease()
        {
            Clear();
        }

        public void OnGui() { }
    }
}
