using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lazy.Manage;
using Lazy.Res.Loader;
using Lazy.Utility;
using Newtonsoft.Json;
using UnityEngine;
using AssetBundleManager = Lazy.Res.Manager.AssetBundleManager;
using EditorLoader = Lazy.Res.Loader.EditorLoader;
using Object = UnityEngine.Object;
using ResourcesManager = Lazy.Res.Manager.ResourcesManager;

namespace Lazy.Res
{
    public delegate void AssetLoadedCallback<T>(T obj)
        where T : Object;

    public class AssetManager : Singleton.Singleton<AssetManager>, IManager
    {
        /// <summary>
        /// * 强制更改资产加载模式为远程（微信小游戏使用）
        /// </summary>
        public static bool ForceRemoteAssetBundle = false;

        public const string DirectorySuffix = "@Directory";

        private AssetManager() { }

        /// <summary>
        /// * 是否试用Editor模式
        /// </summary>
        private bool _isEditorMode;

        public bool IsEditorMode
        {
            get
            {
#if UNITY_EDITOR
                return _isEditorMode
                    || UnityEditor.EditorPrefs.GetBool(
                        Application.dataPath.GetHashCode() + "IsEditorMode",
                        false
                    );
#endif
                return false;
            }
            set { _isEditorMode = value; }
        }

        /// <summary>
        /// * 同步加载资源对象
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="subAssetName"></param>
        /// <param name="mode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T LoadSync<T>(
            string assetName,
            string subAssetName = null,
            AssetAccessMode mode = AssetAccessMode.Unknown
        )
            where T : Object
        {
            var info = GetAssetInfo(assetName, mode);
            if (!IsLegal(ref info))
                return null;

            if (info.AssetType == AssetType.Resource)
            {
                var assetPath = info.AssetPath?[0];
#if UNITY_EDITOR
                if (IsEditorMode)
                    assetPath = info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0];
#endif
                var o = ResourcesManager.Instance.GetAssetObject<T>(assetPath, out _, subAssetName);
                if (o != null)
                    return o;

                if (string.IsNullOrEmpty(subAssetName))
                    return ResourcesManager.Instance.LoadSync<T>(assetPath);

                return ResourcesManager.Instance.LoadAll<T>(assetPath, subAssetName, out _);
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                    return EditorLoadAsset<T>(
                        info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0],
                        subAssetName,
                        out _
                    );
#endif
                var o2 = AssetBundleManager.Instance.GetAssetObject<T>(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out var loader
                );
                if (o2 != null)
                    return o2;

                if (loader == null || loader.AssetBundle == null)
                {
                    AssetBundleManager.Instance.LoadSync(typeof(T), ref info, subAssetName);
                    loader = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                }

                var o = AssetBundleManager.Instance.GetAssetObject<T>(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out _
                );
                if (o != null)
                    return o;
                loader.ExpandSync(info.AssetPath[0], typeof(T), subAssetName);
                return AssetBundleManager.Instance.GetAssetObject<T>(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out _
                );
            }

            return null;
        }

        /// <summary>
        /// * 同步加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="assetType"></param>
        /// <param name="subAssetName"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public Object LoadSync(
            string assetName,
            Type assetType = null,
            string subAssetName = null,
            AssetAccessMode mode = AssetAccessMode.Unknown
        )
        {
            var info = GetAssetInfo(assetName, mode);
            if (!IsLegal(ref info))
                return null;

            if (info.AssetType == AssetType.Resource)
            {
                var assetPath = info.AssetPath?[0];
#if UNITY_EDITOR
                if (IsEditorMode)
                    assetPath = info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0];
#endif
                var o = ResourcesManager.Instance.GetAssetObject(assetPath, out _, subAssetName);
                if (o != null)
                    return o;

                if (string.IsNullOrEmpty(subAssetName))
                    return ResourcesManager.Instance.LoadSync(assetPath, assetType);

                return ResourcesManager.Instance.LoadAll(assetPath, assetType, subAssetName, out _);
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                    return EditorLoadAsset(
                        info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0],
                        out _,
                        assetType,
                        subAssetName
                    );
#endif
                var o2 = AssetBundleManager.Instance.GetAssetObject(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out var loader
                );
                if (o2 != null)
                    return o2;

                if (loader == null || loader.AssetBundle == null)
                {
                    AssetBundleManager.Instance.LoadSync(assetType, ref info, subAssetName);
                    loader = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                }

                var o = AssetBundleManager.Instance.GetAssetObject(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out _
                );
                if (o != null)
                    return o;
                loader.ExpandSync(info.AssetPath[0], assetType, subAssetName);
                return AssetBundleManager.Instance.GetAssetObject(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out _
                );
            }

            return null;
        }

        public ABSLoader LoadAsync<T>(
            string assetName,
            AssetLoadedCallback<T> callback = null,
            string subAssetName = null,
            AssetAccessMode mode = AssetAccessMode.Unknown
        )
            where T : Object
        {
            var info = GetAssetInfo(assetName, mode);
            if (!IsLegal(ref info))
            {
                End();
                return null;
            }

            if (info.AssetType == AssetType.Resource)
            {
                var assetPath = info.AssetPath?[0];
#if UNITY_EDITOR
                if (IsEditorMode)
                    assetPath = info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0];
#endif
                var o = ResourcesManager.Instance.GetAssetObject<T>(
                    assetPath,
                    out var loader,
                    subAssetName
                );
                if (o != null)
                {
                    End(o);
                    return loader;
                }

                if (loader == null || !loader.LoadSuccess || o == null)
                {
                    if (string.IsNullOrEmpty(subAssetName))
                    {
                        return ResourcesManager.Instance.LoadAsync(assetPath, callback);
                    }
                    else
                    {
                        var subAsset = ResourcesManager.Instance.LoadAll<T>(
                            assetPath,
                            subAssetName,
                            out var loader2
                        );
                        End(subAsset);
                        return loader2;
                    }
                }

                return null;
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                {
                    var o = EditorLoadAsset<T>(
                        info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0],
                        subAssetName,
                        out var editorLoader
                    );
                    End(o);
                    return editorLoader;
                }
#endif
                var o2 = AssetBundleManager.Instance.GetAssetObject<T>(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out var loader
                );
                if (o2 != null)
                {
                    End(o2);
                    return loader;
                }

                if (
                    loader == null // # 不存在该加载器
                    || loader.AssetBundle == null // # AssetBundle未加载
                    || loader.GetDependentNamesLoadFinished() < loader.AddDependent() // # 所有依赖还没有加载完
                )
                {
                    loader = AssetBundleManager.Instance.LoadAsync(
                        typeof(T),
                        info,
                        subAssetName,
                        _ =>
                        {
                            End(
                                AssetBundleManager.Instance.GetAssetObject<T>(
                                    info.AssetBundlePath,
                                    info.AssetPath[0],
                                    subAssetName,
                                    out var __
                                )
                            );
                        }
                    );
                    return loader;
                }
                else
                {
                    var o = AssetBundleManager.Instance.GetAssetObject<T>(
                        info.AssetBundlePath,
                        info.AssetPath[0],
                        subAssetName,
                        out var loader3
                    );
                    if (o != null)
                    {
                        End(o);
                        return loader3;
                    }

                    loader.ExpandSync(info.AssetPath[0], typeof(T), subAssetName);
                    End(
                        AssetBundleManager.Instance.GetAssetObject<T>(
                            info.AssetBundlePath,
                            info.AssetPath[0],
                            subAssetName,
                            out var loader4
                        )
                    );
                    return loader4;
                }
            }

            return null;

            void End(T o = null)
            {
                callback?.Invoke(o);
            }
        }

        public ABSLoader LoadAsync(
            string assetName,
            Type assetType = null,
            AssetLoadedCallback<Object> callback = null,
            string subAssetName = null,
            AssetAccessMode mode = AssetAccessMode.Unknown
        )
        {
            var info = GetAssetInfo(assetName, mode);
            if (!IsLegal(ref info))
            {
                End();
                return null;
            }

            if (info.AssetType == AssetType.Resource)
            {
                var assetPath = info.AssetPath?[0];
#if UNITY_EDITOR
                if (IsEditorMode)
                    assetPath = info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0];
#endif
                var o = ResourcesManager.Instance.GetAssetObject(
                    assetPath,
                    out var loader,
                    subAssetName
                );
                if (o != null)
                {
                    End(o);
                    return loader;
                }

                if (loader == null || !loader.LoadSuccess || o == null)
                {
                    if (string.IsNullOrEmpty(subAssetName))
                    {
                        return ResourcesManager.Instance.LoadAsync(assetPath, assetType, callback);
                    }
                    else
                    {
                        var subAsset = ResourcesManager.Instance.LoadAll(
                            assetPath,
                            assetType,
                            subAssetName,
                            out var loader2
                        );
                        End(subAsset);
                        return loader2;
                    }
                }

                return null;
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                {
                    var o = EditorLoadAsset(
                        info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0],
                        out var editorLoader,
                        assetType,
                        subAssetName
                    );
                    End(o);
                    return editorLoader;
                }
#endif
                var o2 = AssetBundleManager.Instance.GetAssetObject(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out var loader
                );
                if (o2 != null)
                {
                    End(o2);
                    return loader;
                }

                if (
                    loader == null
                    || loader.AssetBundle == null
                    || loader.GetDependentNamesLoadFinished() < loader.AddDependent()
                )
                {
                    loader = AssetBundleManager.Instance.LoadAsync(
                        assetType,
                        info,
                        subAssetName,
                        _ =>
                        {
                            End(
                                AssetBundleManager.Instance.GetAssetObject(
                                    info.AssetBundlePath,
                                    info.AssetPath[0],
                                    subAssetName,
                                    out var __
                                )
                            );
                        }
                    );
                    return loader;
                }
                else
                {
                    var o = AssetBundleManager.Instance.GetAssetObject(
                        info.AssetBundlePath,
                        info.AssetPath[0],
                        subAssetName,
                        out var loader3
                    );
                    if (o != null)
                    {
                        End(o);
                        return loader3;
                    }

                    loader3.ExpandSync(info.AssetPath[0], assetType, subAssetName);
                    End(
                        AssetBundleManager.Instance.GetAssetObject(
                            info.AssetBundlePath,
                            info.AssetPath[0],
                            subAssetName,
                            out var loader4
                        )
                    );
                    return loader4;
                }
            }

            return null;

            void End(Object o = null)
            {
                callback?.Invoke(o);
            }
        }

        /// <summary>
        /// * 协程加载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="subAssetName"></param>
        /// <param name="mode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerator LoadCoroutine<T>(
            string assetName,
            string subAssetName = null,
            AssetAccessMode mode = AssetAccessMode.Unknown
        )
            where T : Object
        {
            var info = GetAssetInfo(assetName, mode);
            if (!IsLegal(ref info))
                yield break;

            if (info.AssetType == AssetType.Resource)
            {
                var assetPath = info.AssetPath?[0];
#if UNITY_EDITOR
                if (IsEditorMode)
                    assetPath = info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0];
#endif
                var o = ResourcesManager.Instance.GetAssetObject<T>(assetPath, out _, subAssetName);
                if (o != null)
                {
                    yield return o;
                }
                else
                {
                    if (string.IsNullOrEmpty(subAssetName))
                        yield return ResourcesManager.Instance.LoadCoroutine<T>(assetPath);
                    else
                        yield return ResourcesManager.Instance.LoadAll<T>(
                            assetPath,
                            subAssetName,
                            out _
                        );
                }
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                {
                    var o = EditorLoadAsset<T>(
                        info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0],
                        subAssetName,
                        out _
                    );
                    yield return o;
                    yield break;
                }
#endif
                var o2 = AssetBundleManager.Instance.GetAssetObject<T>(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out var loader
                );
                if (o2 != null)
                    yield return o2;

                if (
                    loader == null
                    || loader.AssetBundle == null
                    || loader.GetDependentNamesLoadFinished() < loader.AddDependent()
                )
                {
                    yield return AssetBundleManager.Instance.LoadCoroutine(
                        typeof(T),
                        info,
                        subAssetName
                    );
                }
                else
                {
                    var o = AssetBundleManager.Instance.GetAssetObject<T>(
                        info.AssetBundlePath,
                        info.AssetPath[0],
                        subAssetName,
                        out _
                    );
                    if (o != null)
                    {
                        yield return o;
                    }
                    else
                    {
                        loader.ExpandSync(info.AssetPath[0], typeof(T), subAssetName);
                        yield return AssetBundleManager.Instance.GetAssetObject<T>(
                            info.AssetBundlePath,
                            info.AssetPath[0],
                            subAssetName,
                            out _
                        );
                        // ! 也可以写成如下
                        // yield return loader.GetAssetObject<T>(subAssetName);
                    }
                }
            }
        }

        public IEnumerator LoadCoroutine(
            string assetName,
            Type assetType = null,
            string subAssetName = null,
            AssetAccessMode mode = AssetAccessMode.Unknown
        )
        {
            var info = GetAssetInfo(assetName, mode);
            if (!IsLegal(ref info))
                yield break;

            if (info.AssetType == AssetType.Resource)
            {
                var assetPath = info.AssetPath?[0];
#if UNITY_EDITOR
                if (IsEditorMode)
                    assetPath = info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0];
#endif
                var o = ResourcesManager.Instance.GetAssetObject(assetPath, out _, subAssetName);
                if (o != null)
                {
                    yield return o;
                }
                else
                {
                    if (string.IsNullOrEmpty(subAssetName))
                        yield return ResourcesManager.Instance.LoadCoroutine(assetPath, assetType);
                    else
                        yield return ResourcesManager.Instance.LoadAll(
                            assetPath,
                            assetType,
                            subAssetName,
                            out _
                        );
                }
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                {
                    var o = EditorLoadAsset(
                        info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0],
                        out _,
                        assetType,
                        subAssetName
                    );
                    yield return o;
                    yield break;
                }
#endif
                var o2 = AssetBundleManager.Instance.GetAssetObject(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out var loader
                );
                if (o2 != null)
                    yield return o2;

                if (
                    loader == null
                    || loader.AssetBundle == null
                    || loader.GetDependentNamesLoadFinished() < loader.AddDependent()
                )
                {
                    yield return AssetBundleManager.Instance.LoadCoroutine(
                        assetType,
                        info,
                        subAssetName
                    );
                }
                else
                {
                    var o = AssetBundleManager.Instance.GetAssetObject(
                        info.AssetBundlePath,
                        info.AssetPath[0],
                        subAssetName,
                        out _
                    );
                    if (o != null)
                    {
                        yield return o;
                    }
                    else
                    {
                        loader.ExpandSync(info.AssetPath[0], assetType, subAssetName);
                        yield return AssetBundleManager.Instance.GetAssetObject(
                            info.AssetBundlePath,
                            info.AssetPath[0],
                            subAssetName,
                            out _
                        );
                    }
                }
            }
        }

        /// <summary>
        /// * 同步加载资源文件夹
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="mode"></param>
        public void LoadDirSync(string assetName, AssetAccessMode mode = AssetAccessMode.Unknown)
        {
            assetName += DirectorySuffix;
            var info = GetAssetInfo(assetName, mode);
            if (!IsLegal(ref info))
                return;

            if (info.AssetType == AssetType.Resource)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                    return;
#endif
                var assetPaths = info.AssetPath;
                if (assetPaths == null || assetPaths.Length <= 0)
                    return;

                foreach (var subAssetName in assetPaths)
                {
                    var subInfo = GetAssetInfo(subAssetName, mode);
                    var assetPath = subInfo.AssetPath?[0];
                    var o = ResourcesManager.Instance.GetAssetObject(assetPath, out _, null);

                    if (o == null)
                        ResourcesManager.Instance.LoadSync(assetPath);
                }
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                    return;
#endif
                foreach (var subAssetName in info.AssetPath)
                {
                    if (string.IsNullOrEmpty(subAssetName))
                        continue;

                    var subInfo = GetAssetInfo(subAssetName, mode);
                    var loader = AssetBundleManager.Instance.GetAssetBundleLoader(
                        subInfo.AssetBundlePath
                    );
                    if (loader == null || loader.AssetBundle == null)
                        AssetBundleManager.Instance.LoadSync(null, ref subInfo, "");
                }
            }
        }

        /// <summary>
        /// * 异步加载资源文件夹
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public DirLoader LoadDirAsync(
            string assetName,
            Action callback = null,
            AssetAccessMode mode = AssetAccessMode.Unknown
        )
        {
            assetName += DirectorySuffix;
            var info = GetAssetInfo(assetName, mode);
            var dirLoader = new DirLoader();
            if (!IsLegal(ref info))
            {
                End();
                return dirLoader;
            }

            if (info.AssetType == AssetType.Resource)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                {
                    End();
                    return dirLoader;
                }
#endif
                var assetPaths = info.AssetPath;
                if (assetPaths == null || assetPaths.Length <= 0)
                {
                    End();
                    return dirLoader;
                }

                var assetCount = 0;
                foreach (var subAssetName in assetPaths)
                {
                    var subInfo = GetAssetInfo(subAssetName, mode);
                    var assetPath = subInfo.AssetPath?[0];
                    var o = ResourcesManager.Instance.GetAssetObject(
                        assetPath,
                        out var loader,
                        null
                    );
                    if (o != null)
                    {
                        dirLoader.Loaders.Add(loader);
                        if (++assetCount >= info.AssetPath?.Length)
                        {
                            End();
                            dirLoader.OnCompleted();
                        }
                    }
                    else
                    {
                        var loader2 = ResourcesManager.Instance.LoadAsync(
                            assetPath,
                            _ =>
                            {
                                if (++assetCount >= info.AssetPath?.Length)
                                {
                                    End();
                                    dirLoader.OnCompleted();
                                }
                            }
                        );
                        dirLoader.Loaders.Add(loader2);
                    }
                }
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                {
                    End();
                    return dirLoader;
                }
#endif
                var assetCount = 0;
                foreach (var subAssetName in info.AssetPath)
                {
                    if (string.IsNullOrEmpty(subAssetName))
                        continue;
                    var subInfo = GetAssetInfo(subAssetName, mode);
                    var loader = AssetBundleManager.Instance.GetAssetBundleLoader(
                        subInfo.AssetBundlePath
                    );
                    if (
                        loader == null
                        || loader.AssetBundle == null
                        || loader.GetDependentNamesLoadFinished() < loader.AddDependent()
                    )
                    {
                        loader = AssetBundleManager.Instance.LoadAsync(
                            null,
                            subInfo,
                            "",
                            _ =>
                            {
                                if (++assetCount >= info.AssetPath?.Length)
                                {
                                    End();
                                    dirLoader.OnCompleted();
                                }
                            }
                        );
                        dirLoader.Loaders.Add(loader);
                    }
                    else
                    {
                        var o = AssetBundleManager.Instance.GetAssetObject(
                            subInfo.AssetBundlePath,
                            subInfo.AssetPath[0],
                            null,
                            out var loader2
                        );
                        if (o != null)
                        {
                            dirLoader.Loaders.Add(loader2);
                            if (++assetCount >= info.AssetPath.Length)
                            {
                                End();
                                dirLoader.OnCompleted();
                            }

                            continue;
                        }

                        loader.ExpandSync(subInfo.AssetPath[0], null, "");
                        dirLoader.Loaders.Add(loader);
                        if (++assetCount >= info.AssetPath.Length)
                        {
                            End();
                            dirLoader.OnCompleted();
                        }
                    }
                }
            }

            return dirLoader;

            void End()
            {
                callback.Fire();
            }
        }

        public IEnumerator LoadDirCoroutine(
            string assetName,
            AssetAccessMode mode = AssetAccessMode.Unknown
        )
        {
            assetName += DirectorySuffix;
            var info = GetAssetInfo(assetName, mode);
            if (!IsLegal(ref info))
                yield break;

            if (info.AssetType == AssetType.Resource)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                    yield break;
#endif
                var assetPaths = info.AssetPath;
                if (assetPaths == null || assetPaths.Length <= 0)
                    yield break;

                foreach (var subAssetName in assetPaths)
                {
                    var subInfo = GetAssetInfo(subAssetName, mode);
                    var assetPath = subInfo.AssetPath?[0];
                    var o = ResourcesManager.Instance.GetAssetObject(assetPath, out _, null);
                    if (o != null)
                        yield return o;
                    else
                        yield return ResourcesManager.Instance.LoadCoroutine(assetPath);
                }
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                    yield break;
#endif
                var assetCount = 0;
                foreach (var subAssetName in info.AssetPath)
                {
                    if (string.IsNullOrEmpty(subAssetName))
                        continue;

                    var subInfo = GetAssetInfo(subAssetName, mode);
                    var loader = AssetBundleManager.Instance.GetAssetBundleLoader(
                        subInfo.AssetBundlePath
                    );
                    if (
                        loader == null
                        || loader.AssetBundle == null
                        || loader.GetDependentNamesLoadFinished() < loader.AddDependent()
                    )
                    {
                        yield return AssetBundleManager.Instance.LoadCoroutine(null, subInfo, null);
                        if (++assetCount >= info.AssetPath.Length)
                            yield break;
                    }
                    else
                    {
                        var o = AssetBundleManager.Instance.GetAssetObject(
                            subInfo.AssetBundlePath,
                            subInfo.AssetPath[0],
                            null,
                            out var loader2
                        );
                        if (o != null)
                        {
                            if (++assetCount >= info.AssetPath.Length)
                                yield break;
                            continue;
                        }

                        loader.ExpandSync(subInfo.AssetPath[0], null, "");
                        if (++assetCount >= info.AssetPath.Length)
                            yield break;
                    }
                }
            }
        }

        /// <summary>
        /// * 根据资源名称同步卸载资源
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="unloadAllLoadedObjects"></param>
        public void UnloadSync(string assetName, bool unloadAllLoadedObjects = false)
        {
#if UNITY_EDITOR
            if (IsEditorMode)
                return;
#endif
            var info = GetAssetInfoFromAssetBundle(assetName);
            if (IsLegal(ref info))
                AssetBundleManager.Instance.UnloadSync(
                    info.AssetBundlePath,
                    unloadAllLoadedObjects
                );

            var remoteInfo = GetAssetInfoFromAssetBundle(assetName, true);
            if (IsLegal(ref remoteInfo))
                AssetBundleManager.Instance.UnloadSync(
                    remoteInfo.AssetBundlePath,
                    unloadAllLoadedObjects
                );

            var resInfo = GetAssetInfoFromResource(assetName);
            if (IsLegal(ref resInfo))
                ResourcesManager.Instance.Unload(resInfo.AssetPath[0], unloadAllLoadedObjects);
        }

        public void UnloadAsync(
            string assetName,
            bool unloadAllLoadedObjects = false,
            AssetBundleLoader.OnUnloadFinished callback = null
        )
        {
#if UNITY_EDITOR
            if (IsEditorMode)
                return;
#endif
            var info = GetAssetInfoFromAssetBundle(assetName);
            if (IsLegal(ref info))
                AssetBundleManager.Instance.UnloadAsync(
                    info.AssetBundlePath,
                    unloadAllLoadedObjects,
                    callback
                );

            var remoteInfo = GetAssetInfoFromAssetBundle(assetName, true);
            if (IsLegal(ref remoteInfo))
                AssetBundleManager.Instance.UnloadAsync(
                    info.AssetBundlePath,
                    unloadAllLoadedObjects,
                    callback
                );

            var resInfo = GetAssetInfoFromResource(assetName);
            if (IsLegal(ref resInfo))
            {
                ResourcesManager.Instance.Unload(resInfo.AssetPath[0], unloadAllLoadedObjects);
                callback?.Invoke();
            }
        }

        /// <summary>
        /// * 如果信息合法,则为真
        /// </summary>
        /// <param name="assetInfo"></param>
        /// <returns></returns>
        public bool IsLegal(ref AssetInfo assetInfo)
        {
#if UNITY_EDITOR
            if (IsEditorMode)
            {
                if (assetInfo.AssetType == AssetType.Resource)
                    if (
                        assetInfo.AssetPath != null
                        || SearchAsset(assetInfo.AssetName, AssetAccessMode.Resource) != null
                    )
                        return true;

                if (assetInfo.AssetType == AssetType.AssetBundle)
                    if (
                        (assetInfo.AssetPath != null && assetInfo.AssetBundlePath != null)
                        || SearchAsset(assetInfo.AssetName, AssetAccessMode.LocalAssetBundle)
                            != null
                    )
                        return true;

                return false;
            }

#endif
            if (assetInfo.AssetType == AssetType.None)
                return false;
            if (assetInfo.AssetType == AssetType.Resource && assetInfo.AssetPath == null)
                return false;

            if (
                assetInfo.AssetType == AssetType.AssetBundle
                && (assetInfo.AssetPath == null || string.IsNullOrEmpty(assetInfo.AssetBundlePath))
            )
                return false;
            return true;
        }

        /// <summary>
        /// * 柑橘提供的资源名称获取信息
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="accessMode"></param>
        /// <returns></returns>
        public AssetInfo GetAssetInfo(
            string assetName,
            AssetAccessMode accessMode = AssetAccessMode.Unknown
        )
        {
            if (ForceRemoteAssetBundle)
                accessMode = AssetAccessMode.RemoteAssetBundle;

            if (accessMode.HasFlag(AssetAccessMode.Resource))
            {
                return GetAssetInfoFromResource(assetName);
            }
            else if (accessMode.HasFlag(AssetAccessMode.LocalAssetBundle))
            {
                return GetAssetInfoFromAssetBundle(assetName);
            }
            else if (accessMode.HasFlag(AssetAccessMode.Unknown))
            {
                var info = GetAssetInfoFromAssetBundle(assetName);
                if (!IsLegal(ref info))
                    info = GetAssetInfoFromResource(assetName);

                if (IsLegal(ref info))
                    return info;
                Log.Log.MsgE("AssetBundle和Resource都找不到指定资源可用的索引：" + assetName);
                return new AssetInfo();
            }
            else if (accessMode.HasFlag(AssetAccessMode.RemoteAssetBundle))
            {
                var info = GetAssetInfoFromAssetBundle(assetName, true);
                if (!IsLegal(ref info))
                    info = GetAssetInfoFromResource(assetName);

                if (IsLegal(ref info))
                    return info;
                Log.Log.MsgE("AssetBundle和Resource都找不到指定远程资源可用的索引：" + assetName);
                return new AssetInfo();
            }

            return new AssetInfo();
        }

        private AssetInfo GetAssetInfoFromResource(string assetName)
        {
            if (ResourceMapping.Mappings.TryGetValue(assetName, out var value))
                return new AssetInfo(AssetType.Resource, assetName, value, null, null);
            Log.Log.MsgE($"Resource找不到指定资源可用的索引: {assetName}");
            return new AssetInfo(AssetType.Resource, assetName);
        }

        private AssetInfo GetAssetInfoFromAssetBundle(string assetName, bool remote = false)
        {
            if (AssetBundleMapping.Mappings.TryGetValue(assetName, out var value))
            {
                if (remote || ForceRemoteAssetBundle)
                    return new AssetInfo(
                        AssetType.AssetBundle,
                        assetName,
                        value.AssetPath,
                        AssetBundlePathHelper.GetRemoteAssetBundleCompletePath(),
                        value.AssetBundleName
                    );
                else
                    return new AssetInfo(
                        AssetType.AssetBundle,
                        assetName,
                        value.AssetPath,
                        AssetBundlePathHelper.GetAssetBundlePathWithoutBundleName(assetName),
                        value.AssetBundleName
                    );
            }

            Log.Log.MsgE($"AssetBundle找不到指定资源可用的索引: {assetName}");
            return new AssetInfo(AssetType.AssetBundle, assetName);
        }

        /// <summary>
        /// * 通过资源名称获取加载器的加载进度
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public float GetLoadProgress(string assetName)
        {
#if UNITY_EDITOR
            if (IsEditorMode)
                return 1f;
#endif
            var progress = 2.1f;
            var assetBundlePath = "";
            var assetBundlePathRemote = "";

            var info = GetAssetInfoFromAssetBundle(assetName);
            if (IsLegal(ref info))
                assetBundlePath = info.AssetBundlePath;

            var remoteInfo = GetAssetInfoFromAssetBundle(assetName, true);
            if (IsLegal(ref remoteInfo))
                assetBundlePathRemote = remoteInfo.AssetBundlePath;

            var resInfo = GetAssetInfoFromResource(assetName);
            if (IsLegal(ref resInfo))
            {
                var resProgress = ResourcesManager.Instance.GetLoadProgress(resInfo.AssetPath[0]);
                if (resProgress > -1f)
                    progress = Mathf.Min(progress, resProgress);
            }

            var bundleProgress = AssetBundleManager.Instance.GetProgress(assetBundlePath);
            if (bundleProgress > -1f)
                progress = Mathf.Min(progress, bundleProgress);

            var bundleProgressRemote = AssetBundleManager.Instance.GetProgress(
                assetBundlePathRemote
            );
            if (bundleProgressRemote > -1f)
                progress = Mathf.Min(progress, bundleProgressRemote);

            if (progress >= 2f)
                progress = 0f;
            return progress;
        }

        public float Progress
        {
            get
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                    return 1f;
#endif
                var progress = 2.1f;
                var abProgress = AssetBundleManager.Instance.Progress;
                if (abProgress > -1f)
                    progress = Mathf.Min(progress, abProgress);

                var resProgress = ResourcesManager.Instance.GetAllLoadProgress();
                if (resProgress > -1f)
                    progress = Mathf.Min(progress, resProgress);

                if (progress >= 2f)
                    progress = 0f;
                return progress;
            }
        }

        /// <summary>
        /// * 获取资源对象 (对于协程异步加载有用)
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="subAssetName"></param>
        /// <param name="mode"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetAssetObject<T>(
            string assetName,
            string subAssetName = null,
            AssetAccessMode mode = AssetAccessMode.Unknown
        )
            where T : Object
        {
            var info = GetAssetInfo(assetName, mode);
            if (info.AssetType == AssetType.Resource)
            {
                var assetPath = info.AssetPath?[0];
#if UNITY_EDITOR
                if (IsEditorMode)
                    assetPath = info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0];
#endif
                return ResourcesManager.Instance.GetAssetObject<T>(assetPath, out _, subAssetName);
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                    return EditorLoadAsset<T>(
                        info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0],
                        subAssetName,
                        out _
                    );
#endif
                var o = AssetBundleManager.Instance.GetAssetObject<T>(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out _
                );
                if (o != null)
                    return o;

                var loader = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                loader.ExpandSync(info.AssetPath[0], typeof(T), subAssetName);
                o = AssetBundleManager.Instance.GetAssetObject<T>(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out _
                );
                if (o != null)
                    return o;
                Log.Log.MsgE("获取不到资产或者类型错误！");
            }

            return null;
        }

        public Object GetAssetObject(
            string assetName,
            Type assetType = null,
            string subAssetName = null,
            AssetAccessMode mode = AssetAccessMode.Unknown
        )
        {
            var info = GetAssetInfo(assetName, mode);

            if (info.AssetType == AssetType.Resource)
            {
                var assetPath = info.AssetPath?[0];
#if UNITY_EDITOR
                if (IsEditorMode)
                    assetPath = info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0];
#endif
                var o = ResourcesManager.Instance.GetAssetObject(assetPath, out _, subAssetName);
                return o;
            }
            else if (info.AssetType == AssetType.AssetBundle)
            {
#if UNITY_EDITOR
                if (IsEditorMode)
                    return EditorLoadAsset(
                        info.AssetPath == null ? SearchAsset(assetName) : info.AssetPath[0],
                        out _,
                        assetType,
                        subAssetName
                    );
#endif
                var o = AssetBundleManager.Instance.GetAssetObject(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out _
                );
                if (o != null)
                    return o;

                var ab = AssetBundleManager.Instance.GetAssetBundleLoader(info.AssetBundlePath);
                ab.ExpandSync(info.AssetPath[0], assetType, subAssetName);
                o = AssetBundleManager.Instance.GetAssetObject(
                    info.AssetBundlePath,
                    info.AssetPath[0],
                    subAssetName,
                    out _
                );
                if (o != null)
                    return o;

                Log.Log.MsgE("获取不到资产或者类型错误！");
            }

            return null;
        }

        public override void OnSingletonInitialize()
        {
            ManagerCenter.Create<AssetBundleManager>(() => AssetBundleManager.Instance);
            ManagerCenter.Create<ResourcesManager>(() => ResourcesManager.Instance);

            var settings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            };
            var jsonContent = "";
            TextAsset abObj = null;
            if (
                File.Exists(
                    Application.persistentDataPath + "/" + nameof(AssetBundleMapping) + ".json"
                )
            )
            // # 存在下载下来的map.json
            {
                jsonContent = FileUtility.SafeReadAllText(
                    $"{Application.persistentDataPath}/{nameof(AssetBundleMapping)}.json"
                );
            }
            else
            {
                abObj = Resources.Load<TextAsset>(nameof(AssetBundleMapping));
                jsonContent = abObj.text;
            }

            AssetBundleMapping.Mappings = JsonConvert.DeserializeObject<
                Dictionary<string, AssetMapping>
            >(jsonContent, settings);

            var resObj = Resources.Load<TextAsset>(nameof(ResourceMapping));
            var resContent = resObj.text;
            ResourceMapping.Mappings = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(
                resContent,
                settings
            );

            if (abObj != null)
                Resources.UnloadAsset(abObj);
            Resources.UnloadAsset(resObj);
        }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroy() { }

#if UNITY_EDITOR
        private List<string> _searchDirs = new();
        private List<string> _resourcesDirs = new();
        private List<string> _assetBundlesDirs = new();
        private Dictionary<string, string> _findAssetPaths = new();
        private Dictionary<string, string> _resourcesFindAssetPaths = new();
        private Dictionary<string, string> _assetBundlesFindAssetPaths = new();

        private string SearchAsset(
            string assetName,
            AssetAccessMode accessMode = AssetAccessMode.Unknown
        )
        {
            if (accessMode == AssetAccessMode.Unknown)
            {
                if (_findAssetPaths.TryGetValue(assetName, out var value))
                    return value;
            }
            else if (accessMode == AssetAccessMode.Resource)
            {
                if (_resourcesFindAssetPaths.TryGetValue(assetName, out var value))
                    return value;
            }
            else if (accessMode == AssetAccessMode.LocalAssetBundle)
            {
                if (_assetBundlesFindAssetPaths.TryGetValue(assetName, out var value))
                    return value;
            }

            if (_searchDirs.Count <= 0)
            {
                // # 获取项目中的所有文件夹路径
                var allFolders = UnityEditor.AssetDatabase.GetAllAssetPaths();
                foreach (var folderPath in allFolders)
                    if (Directory.Exists(folderPath) && folderPath.Contains("/Resources"))
                    {
                        _searchDirs.Add(folderPath);
                        _resourcesDirs.Add(folderPath);
                    }

                _searchDirs.Add(Path.Combine(PathSetting.AssetBundlesPath));
                _assetBundlesDirs.Add(Path.Combine(PathSetting.AssetBundlesPath));
            }

            // # 查找指定资源
            string[] dirs = null;
            if (accessMode == AssetAccessMode.Unknown)
                dirs = _searchDirs.ToArray();
            else if (accessMode == AssetAccessMode.Resource)
                dirs = _resourcesDirs.ToArray();
            else if (accessMode == AssetAccessMode.LocalAssetBundle)
                dirs = _assetBundlesDirs.ToArray();

            var guids = UnityEditor.AssetDatabase.FindAssets(assetName, dirs);
            foreach (var guid in guids)
            {
                // # 将 GUID 转换为路径
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(assetPath).Equals(assetName))
                {
                    if (accessMode == AssetAccessMode.Unknown)
                        _findAssetPaths[assetName] = assetPath;
                    else if (accessMode == AssetAccessMode.Resource)
                        _resourcesFindAssetPaths[assetName] = assetPath;
                    else if (accessMode == AssetAccessMode.LocalAssetBundle)
                        _assetBundlesFindAssetPaths[assetName] = assetPath;

                    Log.Log.MsgD("GET:" + assetPath);
                    return assetPath;
                }
            }

            return null;
        }

        /// <summary>
        /// * 编辑器下加载资源
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="subAssetName"></param>
        /// <param name="loader"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T EditorLoadAsset<T>(string assetPath, string subAssetName, out EditorLoader loader)
            where T : Object
        {
            if (string.IsNullOrEmpty(subAssetName))
            {
                var o = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
                loader = new EditorLoader(o);
                return o;
            }
            else
            {
                var objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var obj in objs)
                    if (obj.name.Equals(subAssetName))
                    {
                        loader = new EditorLoader(obj);
                        return obj as T;
                    }
            }

            loader = null;
            return null;
        }

        private Object EditorLoadAsset(
            string assetPath,
            out EditorLoader loader,
            Type assetType = null,
            string subAssetName = null
        )
        {
            if (string.IsNullOrEmpty(subAssetName))
            {
                if (assetType == null)
                {
                    var o = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    loader = new EditorLoader(o);
                    return o;
                }

                var o2 = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, assetType);
                loader = new EditorLoader(o2);
                return o2;
            }
            else
            {
                var objs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
                foreach (var obj in objs)
                    if (obj.name.Equals(subAssetName))
                    {
                        loader = new EditorLoader(obj);
                        return obj;
                    }
            }

            loader = null;
            return null;
        }
#endif
    }
}
