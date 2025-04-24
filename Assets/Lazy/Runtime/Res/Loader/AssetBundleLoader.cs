using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy
{
    public class AssetBundleLoader : ABSLoader
    {
        /// <summary>
        /// * AssetBundle 包路径
        /// </summary>
        private string _assetBundlePath;

        /// <summary>
        /// * AssetBundle 名称
        /// </summary>
        private string _assetBundleName;

        /// <summary>
        /// * 当前直接想要加载的子资产名称
        /// </summary>
        private string _subAssetName;

        /// <summary>
        /// * AssetBundle
        /// </summary>
        public AssetBundle AssetBundle { get; private set; }

        /// <summary>
        /// * AssetBundle包的Hash
        /// </summary>
        private Hash128 _hash128;

        private readonly string _keyword =
            $"{PathSetting.AssetBundlesName}/{PathSetting.GetPlatformName()}/";

        private List<string> _assetPaths = new();

        private Dictionary<string, Object> _assetObjects = new();

        /// <summary>
        /// * 依赖项名称字典
        /// </summary>
        private Dictionary<string, bool> _dependentNames = new();

        private List<string> _parentBundleNames = new();

        /// <summary>
        /// * 加载器加载类型
        /// </summary>
        private AssetBundleLoaderType _loaderType;

        /// <summary>
        /// * 卸载器卸载类型
        /// </summary>
        private AssetBundleLoaderType _unloadType;

        /// <summary>
        /// * AssetBundle加载状态
        /// </summary>
        private LoaderState _assetBundleLoadState = LoaderState.Idle;

        /// <summary>
        /// * AssetBundle扩展加载状态
        /// </summary>
        private LoaderState _assetBundleExpandState = LoaderState.Idle;

        /// <summary>
        /// * AssetBundle卸载状态
        /// </summary>
        private LoaderState _assetBundleUnloadState = LoaderState.Idle;

        /// <summary>
        /// * 扩展数量
        /// </summary>
        private int _expandCount = 0;

        /// <summary>
        /// * AssetBundle异步下载请求
        /// </summary>
        private DownloadRequest _assetBundleDownloadRequest;

        /// <summary>
        /// * AssetBundle异步本地加载请求
        /// </summary>
        public AssetBundleCreateRequest AssetBundleLoadRequest { get; private set; }

        /// <summary>
        /// * AssetBundle异步本地卸载请求
        /// </summary>
        private AsyncOperation _assetBundleUnloadRequest;

        /// <summary>
        /// * 加载完成事件
        /// </summary>
        private event OnLoadFinished _onLoadCompletedEvent;

        /// <summary>
        /// * 扩展完成事件
        /// </summary>
        private OnExpandFinished _onExpandCompletedEvent;

        /// <summary>
        /// * 卸载完成事件
        /// </summary>
        private OnUnloadFinished _onUnloadCompletedEvent;

        /// <summary>
        /// * AssetBundle包是否加载完成
        /// </summary>
        public bool IsLoaded => _assetBundleLoadState == LoaderState.Loaded;

        /// <summary>
        /// * AssetBundle资产对象的加载是否已完成
        /// </summary>
        public bool IsExpandCompleted => _assetBundleExpandState == LoaderState.Loaded;

        /// <summary>
        /// * AssetBundle卸载是否完成
        /// </summary>
        public bool IsUnloaded => _assetBundleUnloadState == LoaderState.Loaded;

        /// <summary>
        /// * 资产是否已开始卸载(正在卸载进程中)
        /// </summary>
        public bool IsUnloading => _assetBundleUnloadState != LoaderState.Idle;

        /// <summary>
        /// * 初始设置
        /// </summary>
        /// <param name="assetBundlePath"></param>
        public virtual void Setup(string assetBundlePath)
        {
            Clear(true);
            _assetBundlePath = assetBundlePath;
            _assetBundleName = GetSubPath(_assetBundlePath);
            _hash128 = AssetBundleManager.Instance.GetAssetBundleHash(_assetBundleName);
        }

        /// <summary>
        /// * 同步加载AssetBundle
        /// </summary>
        /// <returns></returns>
        public virtual AssetBundle LoadSync()
        {
            ClearUnloadData();
            if (_assetBundleLoadState == LoaderState.Loaded && AssetBundle == null)
                // # 状态虽是加载完成,但没有值的情况直接判定为未加载过
                _assetBundleLoadState = LoaderState.Idle;

            if (_assetBundleLoadState == LoaderState.Loaded)
                // # 真正加载成功,直接返回
                return AssetBundle;

            _loaderType = AssetBundleLoaderType.LocalSync;
            if (URLUtility.IsLegalHttpUri(_assetBundlePath))
            {
#if UNITY_WEBGL
                Log.Log.MsgE("WebGL平台请勿同步加载,自动转异步加载");
                LoadAsync(x =>
                {
                    _assetBundle = x;
                    GetAssetPaths();
                });
#else
                var request = new DownloadRequest(_assetBundlePath, _hash128);
                while (!request.IsFinished)
                    ;
                AssetBundle = request.DownloadedAssetBundle;
                GetAssetPaths();
#endif
            }
            else
            {
                AssetBundle = AssetBundle.LoadFromFile(_assetBundlePath);
                GetAssetPaths();
            }

            _assetBundleLoadState = LoaderState.Loaded;
            return AssetBundle;
        }

        /// <summary>
        /// * 异步加载AssetBundle
        /// </summary>
        /// <param name="callback"></param>
        public virtual void LoadAsync(OnLoadFinished callback = null)
        {
            ClearUnloadData();
            if (_assetBundleLoadState == LoaderState.Loaded && AssetBundle == null)
                // # 状态虽是加载完成,但没有值的情况直接判定为未加载过
                _assetBundleLoadState = LoaderState.Idle;

            _onLoadFinished += callback;

            if (_assetBundleLoadState != LoaderState.Idle)
                return;
            _assetBundleLoadState = LoaderState.Loading;
            if (URLUtility.IsLegalHttpUri(_assetBundlePath))
            {
                _loaderType = AssetBundleLoaderType.RemoteAsync;
                _assetBundleDownloadRequest = new DownloadRequest(_assetBundlePath, _hash128);
                if (_assetBundleDownloadRequest != null)
                    return;
                _assetBundleLoadState = LoaderState.Loaded;
                Log.MsgE($"找不到远程AssetBundle:{_assetBundlePath}");
            }
            else
            {
                _loaderType = AssetBundleLoaderType.LocalAsync;
                AssetBundleLoadRequest = AssetBundle.LoadFromFileAsync(_assetBundlePath);
                if (AssetBundleLoadRequest != null)
                    return;
                _assetBundleLoadState = LoaderState.Loaded;
                Log.MsgE($"找不到本地AssetBundle:{_assetBundlePath}");
            }
        }

        /// <summary>
        /// * 同步扩展资源
        /// * 对于Unity中无法扩展的流场景资源类型，此扩展函数将忽略它，并将它标记为已展开
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="assetType"></param>
        /// <param name="subAssetName"></param>
        public virtual void ExpandSync(string assetPath, Type assetType, string subAssetName = null)
        {
            if (AssetBundle == null)
            {
                _assetBundleExpandState = LoaderState.Idle;
                return;
            }

            if (
                _assetBundleExpandState == LoaderState.Loaded
                && _assetObjects.Count != _assetPaths.Count
            )
                _assetBundleExpandState = LoaderState.Idle;

            if (
                _assetBundleExpandState == LoaderState.Loaded
                && !string.IsNullOrEmpty(subAssetName)
                && string.IsNullOrEmpty(_subAssetName)
            )
                _assetBundleExpandState = LoaderState.Idle;

            if (_assetBundleExpandState == LoaderState.Loaded)
                return;

            _expandCount = 0;
            foreach (var path in _assetPaths)
                if (path.Equals(assetPath))
                    LoadAssetObjectSync(path, assetType, subAssetName);
                else
                    LoadAssetObjectSync(path, subAssetName);

            _expandCount = _assetPaths.Count;
            _assetBundleExpandState = LoaderState.Loaded;
        }

        public virtual void ExpandAsync(
            string assetPath,
            Type assetType,
            string subAssetName = null,
            OnExpandFinished callback = null
        )
        {
            if (AssetBundle == null)
            {
                _assetBundleExpandState = LoaderState.Idle;
                return;
            }

            if (
                _assetBundleExpandState == LoaderState.Loaded
                && _assetObjects.Count != _assetPaths.Count
            )
                _assetBundleExpandState = LoaderState.Idle;

            if (
                _assetBundleExpandState == LoaderState.Loaded
                && string.IsNullOrEmpty(_subAssetName)
                && !string.IsNullOrEmpty(subAssetName)
            )
                _assetBundleExpandState = LoaderState.Idle;

            _onExpandFinished += callback;

            if (_assetBundleExpandState == LoaderState.Idle)
            {
                _expandCount = 0;
                _assetBundleExpandState = LoaderState.Loading;
                for (var i = 0; i < _assetPaths.Count; i++)
                    if (_assetPaths[i].Equals(assetPath))
                        LoadAssetObjectAsync(
                            _assetPaths[i],
                            assetType,
                            subAssetName,
                            OnOneExpandCallBack
                        );
                    else
                        LoadAssetObjectAsync(_assetPaths[i], subAssetName, OnOneExpandCallBack);
            }
        }

        public IEnumerator ExpandCoroutine(
            string assetPath,
            Type assetType,
            string subAssetName = null
        )
        {
            if (AssetBundle == null)
            {
                _assetBundleExpandState = LoaderState.Idle;
                yield break;
            }

            if (
                _assetBundleExpandState == LoaderState.Loaded
                && _assetObjects.Count != _assetPaths.Count
            )
                _assetBundleExpandState = LoaderState.Idle;

            if (
                _assetBundleExpandState == LoaderState.Loaded
                && string.IsNullOrEmpty(_subAssetName)
                && !string.IsNullOrEmpty(subAssetName)
            )
                _assetBundleExpandState = LoaderState.Idle;

            if (_assetBundleExpandState == LoaderState.Idle)
            {
                _expandCount = 0;
                _assetBundleExpandState = LoaderState.Loading;
                for (var i = 0; i < _assetPaths.Count; i++)
                    if (_assetPaths[i].Equals(assetPath))
                        LoadAssetObjectAsync(
                            _assetPaths[i],
                            assetType,
                            subAssetName,
                            OnOneExpandCallBack
                        );
                    else
                        LoadAssetObjectAsync(_assetPaths[i], subAssetName, OnOneExpandCallBack);

                yield return new WaitUntil(() => ExpandProgress >= 1f);
            }
            else if (_assetBundleExpandState == LoaderState.Loading)
            {
                yield return new WaitUntil(() => ExpandProgress >= 1f);
            }
        }

        /// <summary>
        /// * 按资源对象名称同步加载资源对象
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="assetType"></param>
        /// <param name="subAssetName"></param>
        /// <returns></returns>
        public Object LoadAssetObjectSync(
            string assetPath,
            Type assetType,
            string subAssetName = null
        )
        {
            if (AssetBundle == null)
                return null;

            // * 流化场景资产包不需要扩展,但必须通过UnityEngine进行访问 SceneManager
            if (AssetBundle.isStreamedSceneAssetBundle)
                return null;

            var o =
                assetType == null
                    ? AssetBundle.LoadAsset(assetPath)
                    : AssetBundle.LoadAsset(assetPath, assetType);
            SetAssetObject(assetPath, o);

            if (!string.IsNullOrEmpty(subAssetName))
            {
                _subAssetName = subAssetName;
                var objects =
                    assetType == null
                        ? AssetBundle.LoadAssetWithSubAssets(assetPath)
                        : AssetBundle.LoadAssetWithSubAssets(assetPath, assetType);
                foreach (var obj in objects)
                {
                    SetAssetObject(assetPath + obj.name, obj);
                    if (obj.name.Equals(subAssetName))
                        o = obj;
                }
            }

            if (assetType == null)
                return o;

            if (assetType.IsAssignableFrom(o.GetType()))
                return o;

            Log.MsgE($"与输入的资产类型不一致:{assetPath}");
            return null;
        }

        /// <summary>
        /// * 按资源对象名称同步加载资源对象
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="subAssetName"></param>
        /// <returns></returns>
        public Object LoadAssetObjectSync(string assetPath, string subAssetName = null)
        {
            if (AssetBundle == null)
                return null;

            // * 流化场景资产包不需要扩展,但必须通过UnityEngine进行访问 SceneManager
            if (AssetBundle.isStreamedSceneAssetBundle)
                return null;

            var o = AssetBundle.LoadAsset(assetPath);
            SetAssetObject(assetPath, o);

            if (string.IsNullOrEmpty(subAssetName))
                return o;

            _subAssetName = subAssetName;
            var objects = AssetBundle.LoadAssetWithSubAssets(assetPath);
            foreach (var obj in objects)
            {
                SetAssetObject(assetPath + obj.name, obj);
                if (obj.name.Equals(subAssetName))
                    o = obj;
            }

            return o;
        }

        public T LoadAssetObjectSync<T>(string assetPath, string subAssetName = null)
            where T : Object
        {
            if (AssetBundle == null)
                return null;

            // * 流化场景资产包不需要扩展,但必须通过UnityEngine进行访问 SceneManager
            if (AssetBundle.isStreamedSceneAssetBundle)
                return null;

            var o = AssetBundle.LoadAsset<T>(assetPath);
            SetAssetObject(assetPath, o);

            if (!string.IsNullOrEmpty(subAssetName))
            {
                _subAssetName = subAssetName;
                var objects = AssetBundle.LoadAssetWithSubAssets<T>(assetPath);
                foreach (var obj in objects)
                {
                    SetAssetObject(assetPath + obj.name, obj);
                    if (obj.name.Equals(subAssetName))
                        o = obj;
                }
            }

            return o;
        }

        public void LoadAssetObjectAsync(
            string assetPath,
            Type assetType,
            string subAssetName = null,
            AssetLoadedCallback<Object> callback = null
        )
        {
            // * 流化场景资产包不需要扩展,但必须通过UnityEngine进行访问 SceneManager
            if (AssetBundle == null || AssetBundle.isStreamedSceneAssetBundle)
            {
                End();
                return;
            }

            AssetBundleRequest request;
            if (string.IsNullOrEmpty(subAssetName))
            {
                request =
                    assetType == null
                        ? AssetBundle.LoadAssetAsync(assetPath)
                        : AssetBundle.LoadAssetAsync(assetPath, assetType);
            }
            else
            {
                _subAssetName = subAssetName;
                request =
                    assetType == null
                        ? AssetBundle.LoadAssetWithSubAssetsAsync(assetPath)
                        : AssetBundle.LoadAssetWithSubAssetsAsync(assetPath, assetType);
            }

            request.completed += _ =>
            {
                var o = request.asset;
                SetAssetObject(assetPath, o);

                foreach (var asset in request.allAssets)
                {
                    SetAssetObject(assetPath + asset.name, asset);
                    if (asset.name.Equals(subAssetName))
                        o = asset;
                }

                if (assetType == null)
                {
                    End(o);
                }
                else
                {
                    if (assetType.IsAssignableFrom(o.GetType()))
                    {
                        End(o);
                    }
                    else
                    {
                        Log.MsgE($"与输入的资产类型不一致:{assetPath}");
                        End();
                    }
                }
            };
            return;

            void End(Object o = null)
            {
                callback?.Invoke(o);
            }
        }

        public void LoadAssetObjectAsync(
            string assetPath,
            string subAssetName = null,
            AssetLoadedCallback<Object> callback = null
        )
        {
            // * 流化场景资产包不需要扩展,但必须通过UnityEngine进行访问 SceneManager
            if (AssetBundle == null || AssetBundle.isStreamedSceneAssetBundle)
            {
                End();
                return;
            }

            AssetBundleRequest request;
            if (string.IsNullOrEmpty(subAssetName))
            {
                request = AssetBundle.LoadAssetAsync(assetPath);
            }
            else
            {
                _subAssetName = subAssetName;
                request = AssetBundle.LoadAssetWithSubAssetsAsync(assetPath);
            }

            request.completed += _ =>
            {
                var o = request.asset;
                SetAssetObject(assetPath, o);

                foreach (var asset in request.allAssets)
                {
                    SetAssetObject(assetPath + asset.name, asset);
                    if (asset.name.Equals(subAssetName))
                        o = asset;
                }

                End(o);
            };
            return;

            void End(Object o = null)
            {
                callback?.Invoke(o);
            }
        }

        public void LoadAssetObjectAsync<T>(
            string assetPath,
            string subAssetName = null,
            AssetLoadedCallback<T> callback = null
        )
            where T : Object
        {
            // * 流化场景资产包不需要扩展,但必须通过UnityEngine进行访问 SceneManager
            if (AssetBundle == null || AssetBundle.isStreamedSceneAssetBundle)
            {
                End();
                return;
            }

            AssetBundleRequest request;
            if (string.IsNullOrEmpty(subAssetName))
            {
                request = AssetBundle.LoadAssetAsync<T>(assetPath);
            }
            else
            {
                _subAssetName = subAssetName;
                request = AssetBundle.LoadAssetWithSubAssetsAsync<T>(assetPath);
            }

            request.completed += _ =>
            {
                var o = request.asset;
                SetAssetObject(assetPath, o);
                foreach (var asset in request.allAssets)
                {
                    SetAssetObject(assetPath + asset.name, asset);
                    if (asset.name.Equals(subAssetName))
                        o = asset;
                }

                End(o as T);
            };

            return;

            void End(T o = null)
            {
                callback?.Invoke(o);
            }
        }

        /// <summary>
        /// * 添加依赖项名称
        /// </summary>
        /// <param name="name">要添加的名称</param>
        /// <param name="loadFinished">加载是否完成</param>
        /// <returns>添加后的依赖数量</returns>
        public int AddDependent(string name = null, bool loadFinished = false)
        {
            if (string.IsNullOrEmpty(name))
                return _dependentNames.Count;

            if (loadFinished && _dependentNames.ContainsKey(name))
            {
                _dependentNames[name] = true;
            }
            else
            {
                if (!loadFinished)
                    _dependentNames.TryAdd(name, false);
            }

            return _dependentNames.Count;
        }

        /// <summary>
        /// * 移除依赖项
        /// </summary>
        /// <param name="name"></param>
        /// <returns>移除后的依赖数量</returns>
        public int RemoveDependent(string name)
        {
            _dependentNames.Remove(name, out _);
            return _dependentNames.Count;
        }

        /// <summary>
        /// * 获取已加载完成的依赖数量
        /// </summary>
        /// <returns></returns>
        public int GetDependentNamesLoadFinished()
        {
            // # 加载完成的数量
            return _dependentNames.Count(item => item.Value);
        }

        /// <summary>
        /// * 添加父级Bundle名称
        /// </summary>
        /// <param name="name"></param>
        /// <returns>添加后的ParentBundle数量</returns>
        public int AddParentBundle(string name = null)
        {
            if (string.IsNullOrEmpty(name))
                return _parentBundleNames.Count;

            if (!_parentBundleNames.Contains(name))
                _parentBundleNames.Add(name);

            return _parentBundleNames.Count;
        }

        /// <summary>
        /// * 移除父级Bundle名称
        /// </summary>
        /// <param name="name"></param>
        /// <returns>移除后的数量</returns>
        public int RemoveParentBundle(string name)
        {
            if (_parentBundleNames.Contains(name))
                _parentBundleNames.Remove(name);

            return _parentBundleNames.Count;
        }

        /// <summary>
        /// * 检查给定名称是否为父级Bundle
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsParentBundle(string name)
        {
            return _parentBundleNames.Contains(name);
        }

        /// <summary>
        /// * 同步卸载资产
        /// </summary>
        /// <param name="unloadAllLoadedObjects">
        /// * true -> 目标所依赖的所有资产也将被卸载
        /// * false -> 仅卸载目标资产
        /// </param>
        public virtual void UnloadSync(bool unloadAllLoadedObjects = false)
        {
            if (AssetBundle != null)
            {
                AssetBundle.Unload(unloadAllLoadedObjects);
                if (unloadAllLoadedObjects)
                    ClearLoadedData();
            }

            _assetBundleUnloadState = LoaderState.Loaded;
        }

        public virtual void UnloadAsync(
            bool unloadAllLoadedObjects = false,
            OnUnloadFinished callback = null
        )
        {
            if (AssetBundle == null)
                _assetBundleUnloadState = LoaderState.Loaded;

            _onUnloadFinished += callback;

            if (_assetBundleUnloadState == LoaderState.Idle)
            {
                _assetBundleUnloadState = LoaderState.Loading;
                _unloadType = AssetBundleLoaderType.LocalAsync;
                _assetBundleUnloadRequest = AssetBundle.UnloadAsync(unloadAllLoadedObjects);
                ClearLoadedData();
            }
        }

        /// <summary>
        /// * 设置资产对象
        /// </summary>
        /// <param name="assetPath">资产路径</param>
        /// <param name="obj">要设置的对象</param>
        private void SetAssetObject(string assetPath, Object obj)
        {
            if (string.IsNullOrEmpty(assetPath) || obj == null)
            {
                Log.MsgE($"加载资产对象Object为空:{assetPath}");
                return;
            }

            _assetObjects[assetPath] = obj;
        }

        /// <summary>
        /// * 尝试获取资产对象
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool TryGetAsset(string assetPath, out Object obj)
        {
            if (_assetObjects.TryGetValue(assetPath, out var o))
            {
                obj = o;
                return true;
            }

            obj = null;
            return false;
        }

        private string GetSubPath(string fullPath)
        {
            var index = fullPath.IndexOf(_keyword, StringComparison.Ordinal);
            return index != -1
                ?
                // # 找到keyword位置,截取之后的部分
                fullPath[(index + _keyword.Length)..]
                : fullPath;
        }

        /// <summary>
        /// * 获取资源的路径列表
        /// </summary>
        private void GetAssetPaths()
        {
            if (AssetBundle)
            {
                _assetPaths.Clear();
                // * 流化场景资源不需要加载AssetObject
                if (AssetBundle.isStreamedSceneAssetBundle)
                {
                    _assetPaths.Add(string.Empty);
                    return;
                }

                foreach (var assetName in AssetBundle.GetAllAssetNames()) // 获取到的小写: assets/assetbundles/prefabs/cube.prefab
                    _assetPaths.Add(assetName);
            }
        }

        private void OnOneExpandCallBack(Object o = null)
        {
            ++_expandCount;
            if (_expandCount == _assetPaths.Count)
            {
                _assetBundleExpandState = LoaderState.Loaded;
                if (_onExpandCompletedEvent != null)
                {
                    _onExpandCompletedEvent();
                    _onExpandCompletedEvent = null;
                    OnCompleted();
                }
            }
        }

        public override T GetAssetObject<T>(string subAssetName = null)
        {
            if (AssetBundle == null || !IsLoaded || !IsExpandCompleted)
                return null;
            if (string.IsNullOrEmpty(subAssetName))
            {
                if (
                    TryGetAsset(
                        string.IsNullOrEmpty(_subAssetName)
                            ? _assetPaths[0]
                            : _assetPaths[0] + _subAssetName,
                        out var obj
                    )
                )
                    return obj as T;
            }
            else
            {
                if (TryGetAsset(_assetPaths[0] + subAssetName, out var obj))
                    return obj as T;
            }

            return null;
        }

        public override Object GetAssetObject(string subAssetName = null)
        {
            if (AssetBundle == null || !IsLoaded || !IsExpandCompleted)
                return null;
            if (string.IsNullOrEmpty(subAssetName))
            {
                if (
                    TryGetAsset(
                        string.IsNullOrEmpty(_subAssetName)
                            ? _assetPaths[0]
                            : _assetPaths[0] + _subAssetName,
                        out var obj
                    )
                )
                    return obj;
            }
            else
            {
                if (TryGetAsset(_assetPaths[0] + subAssetName, out var obj))
                    return obj;
            }

            return null;
        }

        public virtual void OnUpdate()
        {
            if (_assetBundleLoadState == LoaderState.Loading)
            {
                // # 正在加载AssetBundle
                switch (_loaderType)
                {
                    case AssetBundleLoaderType.LocalAsync:
                        if (AssetBundleLoadRequest != null)
                            if (AssetBundleLoadRequest.isDone)
                            {
                                if (!AssetBundleLoadRequest.assetBundle)
                                {
                                    _assetBundleLoadState = LoaderState.Loaded;
                                    Log.MsgE($"无法加载本地资产捆绑包 {_assetBundlePath} ");
                                }
                                else
                                {
                                    AssetBundle = AssetBundleLoadRequest.assetBundle;
                                    GetAssetPaths();
                                    _assetBundleLoadState = LoaderState.Loaded;
                                }
                            }

                        break;
                    case AssetBundleLoaderType.RemoteAsync:
                        if (_assetBundleDownloadRequest != null)
                            if (_assetBundleDownloadRequest.IsFinished)
                            {
                                if (!_assetBundleDownloadRequest.DownloadedAssetBundle)
                                {
                                    _assetBundleLoadState = LoaderState.Loaded;
                                    Log.MsgE($"无法加载远程资产捆绑包 {_assetBundlePath} ");
                                }
                                else
                                {
                                    AssetBundle = _assetBundleDownloadRequest.DownloadedAssetBundle;
                                    GetAssetPaths();
                                    _assetBundleLoadState = LoaderState.Loaded;
                                }
                            }

                        break;
                }

                if (_assetBundleLoadState == LoaderState.Loaded && _onLoadCompletedEvent != null)
                {
                    _onLoadCompletedEvent(AssetBundle);
                    _onLoadCompletedEvent = null;
                }
            }

            if (_assetBundleUnloadState == LoaderState.Loading)
            {
                switch (_unloadType)
                {
                    case AssetBundleLoaderType.LocalAsync:
                        if (_assetBundleUnloadRequest != null)
                            if (_assetBundleUnloadRequest.isDone)
                                _assetBundleUnloadState = LoaderState.Loaded;

                        break;
                }

                if (
                    _assetBundleUnloadState == LoaderState.Loaded
                    && _onUnloadCompletedEvent != null
                )
                    _onUnloadCompletedEvent();
            }
        }

        /// <summary>
        /// * 加载进度
        /// </summary>
        public float Progress
        {
            get
            {
                switch (_loaderType)
                {
                    case AssetBundleLoaderType.LocalSync:
                        if (_assetBundleLoadState == LoaderState.Loaded)
                            return 1f;
                        break;
                    case AssetBundleLoaderType.LocalAsync:
                        if (AssetBundleLoadRequest != null)
                            return AssetBundleLoadRequest.progress;
                        break;
                    case AssetBundleLoaderType.RemoteAsync:
                        if (_assetBundleDownloadRequest != null)
                            return _assetBundleDownloadRequest.Progress;
                        break;
                }

                return 0f;
            }
        }

        /// <summary>
        /// * 资产展开进度
        /// </summary>
        public float ExpandProgress =>
            _assetPaths == null || _assetPaths.Count == 0
                ? 0
                : _expandCount * 1f / _assetPaths.Count;

        /// <summary>
        /// * 卸载进度
        /// </summary>
        public float UnloadProgress
        {
            get
            {
                switch (_unloadType)
                {
                    case AssetBundleLoaderType.LocalSync:
                        if (_assetBundleUnloadState == LoaderState.Loaded)
                            return 1f;
                        break;
                    case AssetBundleLoaderType.LocalAsync:
                        if (_assetBundleUnloadRequest != null)
                            return _assetBundleUnloadRequest.progress;
                        break;
                }

                return 0f;
            }
        }

        /// <summary>
        /// * 清空已加载的数据
        /// </summary>
        private void ClearLoadedData()
        {
            _unloadType = AssetBundleLoaderType.None;
            _assetBundleLoadState = LoaderState.Idle;
            _assetBundleExpandState = LoaderState.Idle;
            AssetBundle = null;
            _assetObjects.Clear();
            AssetBundleLoadRequest = null;
            _assetBundleDownloadRequest?.Dispose();
            _assetBundleDownloadRequest = null;
            _expandCount = 0;
            _onLoadCompletedEvent = null;
            _onExpandCompletedEvent = null;
        }

        private void ClearUnloadData()
        {
            _unloadType = AssetBundleLoaderType.None;
            _assetBundleUnloadState = LoaderState.Idle;
            _assetBundleUnloadRequest = null;
            _onUnloadCompletedEvent = null;
        }

        /// <summary>
        /// * 异步AssetBundle加载完成的回调
        /// </summary>
        public delegate void OnLoadFinished(AssetBundle assetBundle);

        /// <summary>
        /// * 异步扩展完成的回调
        /// </summary>
        public delegate void OnExpandFinished();

        /// <summary>
        /// * 异步卸载完成的回调
        /// </summary>
        public delegate void OnUnloadFinished();

        private event OnLoadFinished _onLoadFinished
        {
            add
            {
                if (value == null)
                    return;
                if (_assetBundleLoadState == LoaderState.Loaded)
                    value(AssetBundle);
                else
                    _onLoadCompletedEvent += value;
            }
            remove
            {
                if (value != null)
                    _onLoadCompletedEvent -= value;
            }
        }

        private event OnExpandFinished _onExpandFinished
        {
            add
            {
                if (value == null)
                    return;
                if (_assetBundleExpandState == LoaderState.Loaded)
                    value();
                else
                    _onExpandCompletedEvent += value;
            }
            remove
            {
                if (value != null)
                    _onExpandCompletedEvent -= value;
            }
        }

        private event OnUnloadFinished _onUnloadFinished
        {
            add
            {
                if (value == null)
                    return;
                if (_assetBundleUnloadState == LoaderState.Loaded)
                    value();
                else
                    _onUnloadCompletedEvent += value;
            }
            remove
            {
                if (value != null)
                    _onUnloadCompletedEvent -= value;
            }
        }

        public void Clear(bool unloadAllLoadedObjects = false)
        {
            _assetBundlePath = "";
            _subAssetName = "";
            _assetPaths.Clear();
            if (AssetBundle != null)
                UnloadSync(unloadAllLoadedObjects);

            _loaderType = AssetBundleLoaderType.None;
            _unloadType = AssetBundleLoaderType.None;

            _assetBundleLoadState = LoaderState.Idle;
            _assetBundleExpandState = LoaderState.Idle;
            _assetBundleUnloadState = LoaderState.Idle;

            AssetBundleLoadRequest = null;
            _assetBundleDownloadRequest?.Dispose();
            _assetBundleDownloadRequest = null;
            _assetBundleUnloadRequest = null;
            _expandCount = 0;

            _onLoadCompletedEvent = null;
            _onExpandCompletedEvent = null;
            _onUnloadCompletedEvent = null;

            _assetObjects.Clear();
            _parentBundleNames.Clear();
            _dependentNames.Clear();
        }
    }
}
