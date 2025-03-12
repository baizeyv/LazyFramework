using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IPoolable = Lazy.Pool.IPoolable;
using Object = UnityEngine.Object;

namespace Lazy.Res.Loader
{
    public class ResourcesLoader : ABSLoader, IPoolable
    {
        /// <summary>
        /// * Resources 资源路径
        /// </summary>
        private string _resourcePath = "";

        /// <summary>
        /// * 子资源名称
        /// </summary>
        private string _subAssetName = "";

        /// <summary>
        /// * 资源对象
        /// </summary>
        public Object ResourceObject { get; private set; }

        /// <summary>
        /// * 资源名称及资源对应字典
        /// </summary>
        private Dictionary<string, Object> _resourceObjects = new();

        /// <summary>
        /// * 加载器加载方式类型
        /// </summary>
        private ResourceLoaderType _loaderType;

        /// <summary>
        /// * 加载器状态
        /// </summary>
        private LoaderState _loaderState;

        /// <summary>
        /// * Resource 资源异步请求器
        /// </summary>
        private ResourceRequest _resourceRequest;

        /// <summary>
        /// * 通过该加载器加载的预制体的实例
        /// ! 需要手动添加进来,这样才能在释放资源的时候一并销毁预制体的所有实例
        /// </summary>
        private List<GameObject> _prefabInstances;

        public override bool LoadSuccess => _loaderState == LoaderState.Loaded;

        /// <summary>
        /// * 初始设置
        /// </summary>
        /// <param name="resourcePath"></param>
        public virtual void Setup(string resourcePath)
        {
            _resourcePath = resourcePath;
            _loaderType = ResourceLoaderType.None;
            _loaderState = LoaderState.Idle;
            _resourceRequest = null;
        }

        #region LoadSync

        /// <summary>
        /// * 同步加载当前资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T LoadSync<T>()
            where T : Object
        {
            if (_loaderState == LoaderState.Loaded)
                return ResourceObject as T;

            _loaderType = ResourceLoaderType.Sync;
            _loaderState = LoaderState.Loading;
            ResourceObject = Resources.Load<T>(_resourcePath);
            _loaderState = LoaderState.Loaded;
            return (T)ResourceObject;
        }

        /// <summary>
        /// * 同步加载资源
        /// </summary>
        /// <param name="resourceType"></param>
        /// <returns></returns>
        public virtual Object LoadSync(Type resourceType)
        {
            if (_loaderState == LoaderState.Loaded)
                return ResourceObject;
            _loaderType = ResourceLoaderType.Sync;
            _loaderState = LoaderState.Loading;
            ResourceObject = Resources.Load(_resourcePath, resourceType);
            _loaderState = LoaderState.Loaded;
            return ResourceObject;
        }

        /// <summary>
        /// * 同步加载资源
        /// </summary>
        /// <returns></returns>
        public virtual Object LoadSync()
        {
            if (_loaderState == LoaderState.Loaded)
                return ResourceObject;
            _loaderType = ResourceLoaderType.Sync;
            _loaderState = LoaderState.Loading;
            ResourceObject = Resources.Load(_resourcePath);
            _loaderState = LoaderState.Loaded;
            return ResourceObject;
        }

        #endregion

        #region LoadAsync

        public virtual void LoadAsync<T>(AssetLoadedCallback<T> callback = null)
            where T : Object
        {
            if (_loaderState == LoaderState.Idle)
            {
                _loaderType = ResourceLoaderType.Async;
                _loaderState = LoaderState.Loading;
                _resourceRequest = Resources.LoadAsync<T>(_resourcePath);
                _resourceRequest.completed += _ =>
                {
                    ResourceObject = _resourceRequest.asset;
                    _loaderState = LoaderState.Loaded;
                    End(ResourceObject as T);
                };
            }
            else if (_loaderState == LoaderState.Loading)
            {
                _loaderType = ResourceLoaderType.Async;
                _resourceRequest.completed += _ =>
                {
                    End(_resourceRequest.asset as T);
                };
            }
            else if (_loaderState == LoaderState.Loaded)
            {
                _loaderType = ResourceLoaderType.Async;
                callback?.Invoke(ResourceObject as T);
            }

            return;

            void End(T obj = null)
            {
                callback?.Invoke(obj);
                OnCompleted();
            }
        }

        public virtual void LoadAsync(
            Type resourceType,
            AssetLoadedCallback<Object> callback = null
        )
        {
            if (_loaderState == LoaderState.Idle)
            {
                _loaderType = ResourceLoaderType.Async;
                _loaderState = LoaderState.Loading;
                _resourceRequest = Resources.LoadAsync(_resourcePath, resourceType);
                _resourceRequest.completed += _ =>
                {
                    ResourceObject = _resourceRequest.asset;
                    _loaderState = LoaderState.Loaded;
                    if (resourceType.IsAssignableFrom(ResourceObject.GetType()))
                    {
                        End(ResourceObject);
                    }
                    else
                    {
                        Log.Log.MsgE($"Resource Type Error: {_resourcePath}");
                        End();
                    }
                };
            }
            else if (_loaderState == LoaderState.Loading)
            {
                _loaderType = ResourceLoaderType.Async;
                _resourceRequest.completed += _ =>
                {
                    End(_resourceRequest.asset);
                };
            }
            else if (_loaderState == LoaderState.Loaded)
            {
                _loaderType = ResourceLoaderType.Async;
                callback?.Invoke(ResourceObject);
            }

            return;

            void End(Object obj = null)
            {
                callback?.Invoke(obj);
                OnCompleted();
            }
        }

        public virtual void LoadAsync(AssetLoadedCallback<Object> callback = null)
        {
            if (_loaderState == LoaderState.Idle)
            {
                _loaderType = ResourceLoaderType.Async;
                _loaderState = LoaderState.Loading;
                _resourceRequest = Resources.LoadAsync(_resourcePath);
                _resourceRequest.completed += _ =>
                {
                    ResourceObject = _resourceRequest.asset;
                    _loaderState = LoaderState.Loaded;
                    End(ResourceObject);
                };
            }
            else if (_loaderState == LoaderState.Loading)
            {
                _loaderType = ResourceLoaderType.Async;
                _resourceRequest.completed += _ =>
                {
                    End(_resourceRequest.asset);
                };
            }
            else if (_loaderState == LoaderState.Loaded)
            {
                _loaderType = ResourceLoaderType.Async;
                callback?.Invoke(ResourceObject);
            }

            return;

            void End(Object obj = null)
            {
                callback?.Invoke(obj);
                OnCompleted();
            }
        }

        #endregion

        #region LoadCoroutine

        public virtual IEnumerator LoadCoroutine<T>()
            where T : Object
        {
            if (_loaderState == LoaderState.Idle)
            {
                _loaderType = ResourceLoaderType.Async;
                _loaderState = LoaderState.Loading;
                _resourceRequest = Resources.LoadAsync<T>(_resourcePath);
                yield return _resourceRequest;
                ResourceObject = _resourceRequest.asset;
                _loaderState = LoaderState.Loaded;

                yield return ResourceObject;
            }
            else if (_loaderState == LoaderState.Loading)
            {
                _loaderType = ResourceLoaderType.Async;
                yield return new WaitUntil(() => IsLoaded);
                yield return ResourceObject;
            }
            else if (_loaderState == LoaderState.Loaded)
            {
                yield return ResourceObject;
            }
        }

        public virtual IEnumerator LoadCoroutine(Type resourceType = null)
        {
            if (_loaderState == LoaderState.Idle)
            {
                _loaderType = ResourceLoaderType.Async;
                _loaderState = LoaderState.Loading;
                _resourceRequest =
                    resourceType == null
                        ? Resources.LoadAsync(_resourcePath)
                        : Resources.LoadAsync(_resourcePath, resourceType);
                yield return _resourceRequest;
                ResourceObject = _resourceRequest.asset;
                _loaderState = LoaderState.Loaded;

                yield return ResourceObject;
            }
            else if (_loaderState == LoaderState.Loading)
            {
                _loaderType = ResourceLoaderType.Async;
                yield return new WaitUntil(() => IsLoaded);
                yield return ResourceObject;
            }
            else if (_loaderState == LoaderState.Loaded)
            {
                yield return ResourceObject;
            }
        }

        #endregion

        #region LoadAll

        public virtual T LoadAll<T>(string subAssetName = null)
            where T : Object
        {
            if (_loaderState == LoaderState.Loaded)
            {
                if (string.IsNullOrEmpty(subAssetName))
                    return ResourceObject as T;

                if (TryGetAsset(_resourcePath + subAssetName, out var obj))
                    return obj as T;
            }

            _loaderType = ResourceLoaderType.Sync;
            _loaderState = LoaderState.Loading;
            // # 加载同folder名称一样的资源
            ResourceObject = Resources.Load<T>(_resourcePath);
            var result = Resources.LoadAll<T>(_resourcePath);
            Object ro = null;
            foreach (var obj in result)
            {
                SetResourceObject(_resourcePath + obj.name, obj);
                if (!string.IsNullOrEmpty(subAssetName) && obj.name.Equals(subAssetName))
                    ro = obj;
            }

            if (ro != null)
                _subAssetName = subAssetName;

            _loaderState = LoaderState.Loaded;
            return ro != null ? (T)ro : ResourceObject as T;
        }

        public virtual Object LoadAll(Type resourceType = null, string subAssetName = null)
        {
            if (_loaderState == LoaderState.Loaded)
            {
                if (string.IsNullOrEmpty(subAssetName))
                    return ResourceObject;
                if (TryGetAsset(_resourcePath + subAssetName, out var obj))
                    return obj;
            }

            _loaderType = ResourceLoaderType.Sync;
            _loaderState = LoaderState.Loading;
            Object[] result;
            if (resourceType == null)
            {
                ResourceObject = Resources.Load(_resourcePath);
                result = Resources.LoadAll(_resourcePath);
            }
            else
            {
                ResourceObject = Resources.Load(_resourcePath, resourceType);
                result = Resources.LoadAll(_resourcePath, resourceType);
            }

            Object ro = null;
            foreach (var obj in result)
            {
                SetResourceObject(_resourcePath + obj.name, obj);
                if (!string.IsNullOrEmpty(subAssetName) && obj.name.Equals(subAssetName))
                    ro = obj;
            }

            if (ro != null)
                _subAssetName = subAssetName;

            _loaderState = LoaderState.Loaded;
            return ro != null ? ro : ResourceObject;
        }

        #endregion

        public void AddPrefabInstance(params GameObject[] list)
        {
            _prefabInstances ??= new List<GameObject>();
            _prefabInstances.AddRange(list);
        }

        public void SetResourceObject(string resourcePath, Object obj)
        {
            if (string.IsNullOrEmpty(resourcePath) || obj == null)
            {
                Log.Log.MsgE("加载资产对象Object为空，请检查类型和路径：" + resourcePath);
                return;
            }

            _resourceObjects[resourcePath] = obj;
        }

        public bool TryGetAsset(string resourcePath, out Object obj)
        {
            if (_resourceObjects.TryGetValue(resourcePath, out var o))
            {
                obj = o;
                return true;
            }

            obj = null;
            return false;
        }

        public bool IsLoaded => _loaderState == LoaderState.Loaded;

        /// <summary>
        /// * 获取加载进度
        /// </summary>
        public float Progress
        {
            get
            {
                switch (_loaderType)
                {
                    case ResourceLoaderType.Sync:
                        if (_loaderState == LoaderState.Loaded)
                            return 1f;
                        break;
                    case ResourceLoaderType.Async:
                        if (_resourceRequest != null)
                            return _resourceRequest.progress;
                        break;
                    case ResourceLoaderType.None:
                    default:
                        return 0f;
                }

                return 0f;
            }
        }

        public override T GetAssetObject<T>(string subAssetName = null)
        {
            if (!IsLoaded)
                return null;

            if (string.IsNullOrEmpty(subAssetName))
            {
                if (string.IsNullOrEmpty(_subAssetName))
                    return ResourceObject as T;

                if (TryGetAsset(_resourcePath + _subAssetName, out var obj))
                    return obj as T;
            }
            else
            {
                if (TryGetAsset(_resourcePath + subAssetName, out var obj))
                    return obj as T;
                if (string.IsNullOrEmpty(_subAssetName))
                    return ResourceObject as T;
            }

            return null;
        }

        public override Object GetAssetObject(string subAssetName = null)
        {
            if (IsLoaded)
            {
                if (string.IsNullOrEmpty(subAssetName))
                {
                    if (string.IsNullOrEmpty(_subAssetName))
                        return ResourceObject;

                    if (TryGetAsset(_resourcePath + _subAssetName, out var obj))
                        return obj;
                }
                else
                {
                    if (TryGetAsset(_resourcePath + subAssetName, out var obj))
                        return obj;
                    if (string.IsNullOrEmpty(_subAssetName))
                        return ResourceObject;
                }
            }

            return null;
        }

        public bool Is(Object obj)
        {
            if (obj == null)
                return false;
            if (ResourceObject != null && string.IsNullOrEmpty(_subAssetName))
                return ResourceObject == obj;
            return _resourceObjects != null && _resourceObjects.ContainsValue(obj);
        }

        void IPoolable.Reset()
        {
            _resourcePath = "";
            _subAssetName = "";
            if (ResourceObject != null)
                Resources.UnloadAsset(ResourceObject);
            foreach (var item in _resourceObjects.Values)
                if (item != null)
                    Resources.UnloadAsset(item);

            if (_prefabInstances != null)
                foreach (var item in _prefabInstances)
                    Object.Destroy(item);

            _resourceObjects.Clear();
            _loaderType = ResourceLoaderType.None;
            _loaderState = LoaderState.Idle;
            _resourceRequest = null;
        }
    }
}
