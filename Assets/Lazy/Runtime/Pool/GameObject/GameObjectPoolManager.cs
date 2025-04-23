using System;
using System.Collections.Generic;
using System.Linq;
using Lazy;
using Lazy.Event;
using Lazy.Manage;
using Lazy.Pool.GameObject.Data;
using Lazy.Pool.GameObject.Enums;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Pool.GameObject
{
    public class GameObjectPoolManager : Singleton.Singleton<GameObjectPoolManager>, IManager
    {
        internal GlobalPoolInstaller installer = null;

        internal PoolMode poolMode = PoolConstant.DefaultPoolMode;
        internal bool checkForPrefab = false;
        internal bool checkClonesForNull = true;
        internal bool despawnPersistentClonesOnDestroy = true;

        internal bool isApplicationQuitting = false;

        internal bool hasPoolInitialized = false;

        /// <summary>
        /// * 从池中取出的克隆体的字典 Key->克隆体 Value->克隆体对应的Poolable
        /// </summary>
        internal readonly Dictionary<UnityEngine.GameObject, GameObjectPoolable> ClonesMap =
            new(PoolConstant.DefaultClonesMapCapacity);

        internal readonly PoolList<DespawnRequest> DespawnRequests =
            new(PoolConstant.DefaultDespawnRequestsCapacity);

        /// <summary>
        /// * Persistent Pool Dictionary
        /// </summary>
        private readonly Dictionary<UnityEngine.GameObject, GameObjectPool> _persistentPoolsMap =
            new(PoolConstant.DefaultPersistentPoolsCapacity);

        /// <summary>
        /// * 所有池子的映射字典 Key->预制体 Value->预制体对应的池子
        /// </summary>
        private readonly Dictionary<UnityEngine.GameObject, GameObjectPool> _allPoolsMap =
            new(PoolConstant.DefaultPersistentPoolsCapacity);

        private readonly List<ISpawnable> _spawnableItemComponents =
            new(PoolConstant.DefaultPoolableInterfacesCapacity);

        private readonly List<IDespawnable> _despawnableItemComponents =
            new(PoolConstant.DefaultPoolableInterfacesCapacity);

        /// <summary>
        /// * 实例化一个物体时的回调
        /// </summary>
        public readonly SimpleEvent<UnityEngine.GameObject> GameObjectInstantiated = new();

        private readonly object _securityLock = new();

        private CapacityReachedBehaviour CapacityReachedBehaviour =>
            hasPoolInitialized
                ? installer.capacityReachedBehaviour
                : PoolConstant.DefaultCapacityReachedBehaviour;

        private DespawnType DespawnType =>
            hasPoolInitialized ? installer.despawnType : PoolConstant.DefaultDespawnType;

        private CallbackType CallbackType =>
            hasPoolInitialized ? installer.callbackType : PoolConstant.DefaultCallbackType;

        private ReactionOnRepeatedDelayedDespawn ReactionOnRepeatedDelayedDespawn =>
            hasPoolInitialized
                ? installer.reactionOnRepeatedDelayedDespawn
                : PoolConstant.DefaultDelayedDespawnHandleType;

        private int Capacity =>
            hasPoolInitialized ? installer.capacity : PoolConstant.DefaultCapacity;

        private bool Persistent => !hasPoolInitialized || installer.dontDestroyOnLoad;

        private bool Warnings => !hasPoolInitialized || installer.warning;

        private GameObjectPoolManager() { }

        public void InstallPools(PoolsConfig config)
        {
#if DEBUG
            if (config == null)
                throw new ArgumentNullException(nameof(config));
#endif
            // # 预配置池的数量
            var count = config.Configs.Count;
            for (var i = 0; i < count; i++)
            {
                var cfg = config.Configs[i];
                if (!cfg.Enabled)
                    continue;
                var prefab = cfg.Prefab;
#if DEBUG
                if (prefab == null)
                {
                    Log.Log.MsgE(
                        $"名称为{nameof(PoolsConfig)}的'{config}'预设中有一个或多个空的预制体!",
                        config
                    );
                    continue;
                }
#endif
                var preloadSize = Mathf.Clamp(cfg.PreloadSize, 0, cfg.Capacity);
                if (!TryGetPoolByPrefab(prefab, out var pool))
                {
                    pool = CreateNewGameObjectPool(prefab);
                    SetupNewPool(
                        pool,
                        prefab,
                        cfg.CapacityReachedBehaviour,
                        cfg.DespawnType,
                        cfg.CallbackType,
                        cfg.Capacity,
                        preloadSize,
                        cfg.DontDestroyOnLoad,
                        cfg.Warning
                    );
                }
                else
                {
                    if (cfg.DontDestroyOnLoad && pool.HasRegisteredAsPersistent)
                        continue;
#if DEBUG
                    Log.Log.MsgE(
                        $"您正在尝试通过{nameof(PoolsConfig)} '{config}'安装的池 '{pool}' 已经存在!",
                        pool
                    );
#endif
                }
            }
        }

        private void InvokeCallback<T>(
            UnityEngine.GameObject gameObject,
            CallbackType callbackType,
            Action<T> poolableCallback,
            List<T> listForComponentsCaching,
            string messageKey
        )
        {
            switch (callbackType)
            {
                case CallbackType.Interface:
                    InvokeGameObjectPoolEvents(
                        gameObject,
                        listForComponentsCaching,
                        poolableCallback,
                        false
                    );
                    break;
                case CallbackType.InterfaceInChildren:
                    InvokeGameObjectPoolEvents(
                        gameObject,
                        listForComponentsCaching,
                        poolableCallback,
                        true
                    );
                    break;
                case CallbackType.SendMessage:
                    gameObject.SendMessage(messageKey, SendMessageOptions.DontRequireReceiver);
                    break;
                case CallbackType.BroadcastMessage:
                    gameObject.BroadcastMessage(messageKey, SendMessageOptions.DontRequireReceiver);
                    break;
                case CallbackType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(callbackType));
            }
        }

        private void InvokeGameObjectPoolEvents<T>(
            UnityEngine.GameObject gameObject,
            List<T> listForComponentCaching,
            Action<T> callback,
            bool inChildren
        )
        {
            if (inChildren)
                gameObject.GetComponentsInChildren(listForComponentCaching);
            else
                gameObject.GetComponents(listForComponentCaching);

            var count = listForComponentCaching.Count;
            for (var i = 0; i < count; i++)
                callback.Fire(listForComponentCaching[i]);
        }

        private void FireCallbackOnSpawn(GameObjectPoolable poolable)
        {
            if (poolable.Pool.CallbackType == CallbackType.None)
                return;
            InvokeCallback(
                poolable.GameObject,
                poolable.Pool.callbackType,
                x => x.OnSpawn(),
                _spawnableItemComponents,
                PoolConstant.OnSpawnMessageName
            );
        }

        private void FireCallbackOnDespawn(GameObjectPoolable poolable)
        {
            if (poolable.Pool.CallbackType == CallbackType.None)
                return;
            InvokeCallback(
                poolable.GameObject,
                poolable.Pool.callbackType,
                x => x.OnDespawn(),
                _despawnableItemComponents,
                PoolConstant.OnDespawnMessageName
            );
        }

        internal void DespawnImmediately(GameObjectPoolable poolable)
        {
            if (poolable.IsSetup)
            {
                if (poolable.Status == PoolableStatus.SpawnedOverCapacity)
                {
                    if (
                        poolable.Pool.CapacityReachedBehaviour
                        == CapacityReachedBehaviour.InstantiateWithCallback
                    )
                        FireCallbackOnDespawn(poolable);

                    poolable.Dispose(true);
                    return;
                }

                FireCallbackOnDespawn(poolable);

                poolable.Pool.Free(poolable);
                poolable.Pool.FireGameObjectDespawnedCallback(poolable.GameObject);
                poolable.Status = PoolableStatus.Despawned;
            }
            else
            {
#if DEBUG
                Log.Log.MsgD(
                    $"可池化对象 '{poolable.GameObject}' 尚未设置并将被销毁！",
                    poolable.GameObject
                );
#endif
                poolable.Dispose(true);
            }
        }

        internal bool HasPoolRegisteredAsPersistent(GameObjectPool pool)
        {
            return _persistentPoolsMap.ContainsKey(pool.prefab);
        }

        internal void RegisterPersistentPool(GameObjectPool pool)
        {
            if (pool.dontDestroyOnLoad)
            {
                if (!_persistentPoolsMap.ContainsKey(pool.prefab))
                {
                    _persistentPoolsMap.Add(pool.prefab, pool);
                }
#if DEBUG
                else
                {
                    if (pool.sendWarnings)
                        Log.Log.MsgD($"您正在尝试注册持久池 '{pool.name}' 两次！", pool);
                }
#endif
            }
        }

        internal void RegisterPool(GameObjectPool pool)
        {
            if (!_allPoolsMap.ContainsKey(pool.prefab))
                _allPoolsMap.Add(pool.prefab, pool);
#if DEBUG
            else
                Log.Log.MsgE(
                    $"您正在尝试注册另一个使用相同预制体 '{pool.prefab}' 的池 '{pool.name}'!",
                    pool
                );
#endif
        }

        internal void UnregisterPool(GameObjectPool pool)
        {
            if (!pool._isSetup)
                return;
            if (pool.dontDestroyOnLoad)
                _persistentPoolsMap.Remove(pool.prefab);
            _allPoolsMap.Remove(pool.prefab);
        }

        internal void ResetPool()
        {
            ResetLists();
            ResetClonesDictionary();
            HandlePersistentPoolsOnDestroy();
            hasPoolInitialized = false;
        }

        private void HandlePersistentPoolsOnDestroy()
        {
            if (isApplicationQuitting)
                return;
            if (!despawnPersistentClonesOnDestroy)
                return;
            if (_persistentPoolsMap.Count == 0)
                return;

            foreach (var pool in _persistentPoolsMap.Values)
                pool.DespawnAllClones();
        }

        private void ResetLists()
        {
            ClearListAndSetCapacity(
                _spawnableItemComponents,
                PoolConstant.DefaultPoolableInterfacesCapacity
            );
            ClearListAndSetCapacity(
                _despawnableItemComponents,
                PoolConstant.DefaultPoolableInterfacesCapacity
            );
            ClearListAndSetCapacity(DespawnRequests, PoolConstant.DefaultDespawnRequestsCapacity);
        }

        private void ResetClonesDictionary()
        {
            if (isApplicationQuitting)
                ClonesMap.Clear();
        }

        private void ClearListAndSetCapacity<T>(List<T> list, int capacity)
        {
            list.Clear();
            list.Capacity = capacity;
        }

        private void ClearListAndSetCapacity(PoolList<DespawnRequest> list, int capacity)
        {
            list.Clear();
            list.SetCapacity(capacity);
        }

        private void InitializePool()
        {
            lock (_securityLock)
            {
                if (installer == null)
                    if (!TryFindPoolInstallerInstanceAsSingle(out installer))
                    {
                        CreateInstallerInstance();
#if DEBUG
                        Log.Log.MsgD(
                            $"<{nameof(GlobalPoolInstaller)}> 实例已自动创建。也可以手动添加以修改默认参数。"
                        );
#endif
                    }

                hasPoolInitialized = true;
            }
        }

        private void CreateInstallerInstance()
        {
            installer = GlobalPoolInstaller.Instance;
        }

        /// <summary>
        /// * 尝试获取对象池安装器
        /// </summary>
        /// <param name="installer"></param>
        /// <returns></returns>
        private bool TryFindPoolInstallerInstanceAsSingle(out GlobalPoolInstaller installer)
        {
#if UNITY_6000_0_OR_NEWER
            var ins = Object.FindObjectsByType<GlobalPoolInstaller>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
#else
            var ins = Object.FindObjectsOfType<GlobalPoolInstaller>();
#endif
            var length = ins.Length;
            if (length > 0)
            {
#if DEBUG
                if (length > 1)
                {
                    for (var i = 1; i < length; i++)
                        Object.Destroy(ins[i]);
                    Log.Log.MsgE($"场景中 {nameof(GlobalPoolInstaller)} 实例的数量大于一个！");
                }
#endif
                installer = ins[0];
                return true;
            }

            installer = null;
            return false;
        }

        private bool CanFirePoolAction()
        {
            if (isApplicationQuitting)
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorSettings.enterPlayModeOptionsEnabled && installer == null)
                    Log.Log.MsgE($"<{nameof(GlobalPoolInstaller)}> 实例为空！");
#endif
                return false;
            }

            if (!hasPoolInitialized)
            {
#if DEBUG
                if (!Application.isPlaying)
                    Log.Log.MsgE("在应用程序未运行时，您正在尝试执行生成或取消生成操作！");
#endif
                InitializePool();
            }

            return true;
        }

        private GameObjectPool GetPoolByPrefabOrCreate(UnityEngine.GameObject prefab)
        {
            if (!TryGetPoolByPrefab(prefab, out var pool))
            {
                pool = CreateNewGameObjectPool(prefab);
                SetupNewPool(
                    pool,
                    prefab,
                    CapacityReachedBehaviour,
                    DespawnType,
                    CallbackType,
                    Capacity,
                    PoolConstant.NewPoolPreloadSize,
                    Persistent,
                    Warnings
                );
            }

            return pool;
        }

        private GameObjectPool CreateNewGameObjectPool(UnityEngine.GameObject prefab)
        {
            return new UnityEngine.GameObject(
                $"[{nameof(GameObjectPoolManager)}] {prefab.name}"
            ).AddComponent<GameObjectPool>();
        }

        private void SetupNewPool(
            GameObjectPool pool,
            UnityEngine.GameObject prefab,
            CapacityReachedBehaviour capacityReachedBehaviour,
            DespawnType despawnType,
            CallbackType callbackType,
            int capacity,
            int preloadSize,
            bool persistent,
            bool warning
        )
        {
            pool.dontDestroyOnLoad = persistent;
            pool.SetWarning(warning);
            pool.SetCapacity(capacity);
            pool.SetCallbackType(callbackType);
            pool.SetDespawnType(despawnType);
            pool.SetCapacityReachedBehaviour(capacityReachedBehaviour);
            pool.TrySetup(prefab);
            pool.PopulatePool(preloadSize);
        }

        private void SetPoolableNullParent(GameObjectPoolable poolable)
        {
            poolable.Transform.SetParent(null, false);
        }

        private void SetupTransform(
            GameObjectPoolable poolable,
            GameObjectPool pool,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            bool worldPositionStays = false
        )
        {
            if (poolMode == PoolMode.Safety)
                SetPoolableNullParent(poolable);
            else
                CheckPoolableForLightweightTransformSetup(pool, poolable);

            poolable.Transform.localScale = pool._regularPrefabScale;
            poolable.Transform.SetPositionAndRotation(position, rotation);
            poolable.Transform.SetParent(parent, worldPositionStays);
        }

        private void CheckPoolableForLightweightTransformSetup(
            GameObjectPool pool,
            GameObjectPoolable poolable
        )
        {
            if (pool.capacityReachedBehaviour == CapacityReachedBehaviour.Recycle)
            {
                SetPoolableNullParent(poolable);
                return;
            }

            if (pool.despawnType == DespawnType.OnlyDeactivate)
            {
                SetPoolableNullParent(poolable);
                return;
            }
#if DEBUG
            if (poolable.Pool._cachedTransform.lossyScale != Vector3.one)
            {
                Log.Log.MsgE(
                    $"池及其父物体在 F8 池 '{nameof(PoolMode.Performance)}' 模式下必须具有相同的缩放，即 'Vector3.one'！",
                    poolable.Pool
                );
                SetPoolableNullParent(poolable);
            }
#endif
        }

        private UnityEngine.GameObject DefaultSpawn(
            UnityEngine.GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent,
            bool worldPositionStays,
            out bool haveToGetComponent
        )
        {
            if (!CanFirePoolAction())
            {
#if DEBUG
                Log.Log.MsgE($"在应用程序退出时，您正在尝试生成预制体 '{prefab}'！", prefab);
#endif
                haveToGetComponent = false;
                return null;
            }

            var pool = GetPoolByPrefabOrCreate(prefab);
            pool.Get(out var arguments);
            if (arguments.IsResultNullable)
            {
                haveToGetComponent = false;
                return null;
            }
#if DEBUG
            if (checkClonesForNull)
                if (arguments.Poolable.GameObject == null)
                    Log.Log.MsgE(
                        $"您正在尝试生成一个已经在没有 {nameof(GameObjectPoolManager)} 的情况下被销毁的克隆！预制体: '{prefab}'",
                        pool
                    );
#endif
            if (arguments.Poolable.Status == PoolableStatus.Despawned)
                arguments.Poolable.GameObject.SetVisible(true);

            SetupTransform(
                arguments.Poolable,
                pool,
                position,
                rotation,
                parent,
                worldPositionStays
            );
            pool.FireGameObjectSpawnedCallback(arguments.Poolable.GameObject);

            if (arguments.Poolable.Status == PoolableStatus.SpawnedOverCapacity)
            {
                if (
                    pool.capacityReachedBehaviour
                    == CapacityReachedBehaviour.InstantiateWithCallback
                )
                    FireCallbackOnSpawn(arguments.Poolable);
            }
            else
            {
                arguments.Poolable.Status = PoolableStatus.Spawned;
                FireCallbackOnSpawn(arguments.Poolable);
            }

            haveToGetComponent = true;
            return arguments.Poolable.GameObject;
        }

        private void DefaultDespawn(UnityEngine.GameObject gameObject, float delay = 0f)
        {
            if (!CanFirePoolAction())
            {
#if DEBUG
                Log.Log.MsgE($"在应用程序退出时，您正在尝试取消生成 '{gameObject}'！", gameObject);
#endif
                return;
            }

            if (ClonesMap.TryGetValue(gameObject, out var poolable))
            {
                if (poolable.Status == PoolableStatus.Despawned)
                {
#if DEBUG
                    if (poolable.Pool.sendWarnings)
                        Log.Log.MsgD("您要取消生成的游戏对象已经被取消生成！", gameObject);
#endif
                    return;
                }

                if (delay > 0f)
                    DespawnWithDelay(poolable, delay);
                else
                    DespawnImmediately(poolable);
            }
            else
            {
#if DEBUG
                Log.Log.MsgD(
                    $"'{gameObject}' 未使用 {nameof(GameObjectPoolManager)}（或池已销毁）生成，并将被销毁！",
                    gameObject
                );
#endif
                Object.Destroy(gameObject, delay);
            }
        }

        private void DespawnWithDelay(GameObjectPoolable poolable, float delay)
        {
            var reaction = ReactionOnRepeatedDelayedDespawn;
            if (reaction == ReactionOnRepeatedDelayedDespawn.Ignore)
            {
                CreateDespawnRequest(poolable, delay);
            }
            else
            {
                if (HasDespawnRequest(poolable, out var index))
                {
                    ref var request = ref DespawnRequests.Components[index];
                    switch (reaction)
                    {
                        case ReactionOnRepeatedDelayedDespawn.ResetDelay:
                            ResetDespawnDelay(ref request, delay);
                            break;
                        case ReactionOnRepeatedDelayedDespawn.ResetDelayIfNewTimeIsLess:
                            ResetDespawnDelayIfNewTimeIsLess(ref request, delay);
                            break;
                        case ReactionOnRepeatedDelayedDespawn.ResetDelayIfNewTimeIsGreater:
                            ResetDespawnDelayIfNewTimeIsGreater(ref request, delay);
                            break;
                        case ReactionOnRepeatedDelayedDespawn.ThrowException:
#if DEBUG
                            if (HasDespawnRequest(poolable, out _))
                                Log.Log.MsgE(
                                    "延迟取消生成请求已经存在于该克隆！",
                                    poolable.GameObject
                                );
#endif
                            break;
                    }
                }
                else
                {
                    CreateDespawnRequest(poolable, delay);
                }
            }
        }

        private void ResetDespawnDelayIfNewTimeIsGreater(ref DespawnRequest request, float delay)
        {
            if (delay > request.TimeToDespawn)
                request.TimeToDespawn = delay;
        }

        private void ResetDespawnDelayIfNewTimeIsLess(ref DespawnRequest request, float delay)
        {
            if (delay < request.TimeToDespawn)
                request.TimeToDespawn = delay;
        }

        private void ResetDespawnDelay(ref DespawnRequest request, float delay)
        {
            request.TimeToDespawn = delay;
        }

        private bool HasDespawnRequest(GameObjectPoolable poolable, out int id)
        {
            for (var i = 0; i < DespawnRequests.Count; i++)
                if (DespawnRequests.Components[i].Poolable == poolable)
                {
                    id = i;
                    return true;
                }

            id = 0;
            return false;
        }

        private void CreateDespawnRequest(GameObjectPoolable poolable, float delay)
        {
            DespawnRequests.Add(new DespawnRequest { Poolable = poolable, TimeToDespawn = delay });
        }

        private void GetPositionAndRotationByParent(
            UnityEngine.GameObject prefab,
            Transform parent,
            out Vector3 position,
            out Quaternion rotation
        )
        {
            if (parent != null)
            {
                var prefabTransform = prefab.transform;
                position = prefabTransform.position;
                rotation = prefabTransform.rotation;
            }
            else
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
            }
        }

        private void DestroyPoolableWithGameObject(UnityEngine.GameObject clone, bool immediately)
        {
            if (ClonesMap.TryGetValue(clone, out var poolable))
            {
                if (poolable.IsSetup)
                {
                    poolable.Pool.UnRegisterPoolable(poolable);
                    poolable.Dispose(immediately);
                }
#if DEBUG
                else
                {
                    Log.Log.MsgE($"克隆 '{clone}' 尚未设置！", clone);
                }
#endif
            }
            else
            {
#if DEBUG
                Log.Log.MsgD(
                    $"克隆 '{clone}' 并非由 {nameof(GameObjectPoolManager)} 生成！",
                    clone
                );
#endif
                Object.Destroy(clone);
            }
        }

        #region API

        /// <summary>
        /// * Spawn a game object by prefab name
        /// </summary>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        public UnityEngine.GameObject Spawn(string prefabName)
        {
            var pool = GetPoolByPrefabName(prefabName);
            if (!pool)
            {
                Log.Log.MsgE("对象池未创建，通过名称生成对象失败。");
                return null;
            }

            var prefabTransform = pool.AttachedPrefab.transform;
            return DefaultSpawn(
                pool.AttachedPrefab,
                prefabTransform.localPosition,
                prefabTransform.localRotation,
                null,
                false,
                out _
            );
        }

        /// <summary>
        /// * Spawn a game object
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public UnityEngine.GameObject Spawn(UnityEngine.GameObject prefab)
        {
            var prefabTransform = prefab.transform;
            return DefaultSpawn(
                prefab,
                prefabTransform.localPosition,
                prefabTransform.localRotation,
                null,
                false,
                out _
            );
        }

        public UnityEngine.GameObject Spawn(
            UnityEngine.GameObject prefab,
            Vector3 position,
            Quaternion rotation
        )
        {
            return DefaultSpawn(prefab, position, rotation, null, false, out _);
        }

        public UnityEngine.GameObject Spawn(
            UnityEngine.GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent
        )
        {
            if (parent != null)
            {
                position = parent.InverseTransformPoint(position);
                rotation = Quaternion.Inverse(parent.rotation) * rotation;
            }

            return DefaultSpawn(prefab, position, rotation, parent, false, out _);
        }

        public UnityEngine.GameObject Spawn(
            UnityEngine.GameObject prefab,
            Transform parent,
            bool worldPositionStays = false
        )
        {
            GetPositionAndRotationByParent(prefab, parent, out var position, out var rotation);
            return DefaultSpawn(prefab, position, rotation, parent, worldPositionStays, out _);
        }

        /// <summary>
        /// * Spawn a game object as T component
        /// </summary>
        /// <param name="prefab"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Spawn<T>(T prefab)
            where T : Component
        {
            var prefabTransform = prefab.transform;
            var spawned = DefaultSpawn(
                prefab.gameObject,
                prefabTransform.localPosition,
                prefabTransform.localRotation,
                null,
                false,
                out var haveToGetComponent
            );
            return haveToGetComponent ? spawned.GetComponent<T>() : null;
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation)
            where T : Component
        {
            var spawned = DefaultSpawn(
                prefab.gameObject,
                position,
                rotation,
                null,
                false,
                out var haveToGetComponent
            );
            return haveToGetComponent ? spawned.GetComponent<T>() : null;
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent)
            where T : Component
        {
            if (parent != null)
            {
                position = parent.InverseTransformPoint(position);
                rotation = Quaternion.Inverse(parent.rotation) * rotation;
            }

            var spawned = DefaultSpawn(
                prefab.gameObject,
                position,
                rotation,
                parent,
                false,
                out var haveToGetComponent
            );
            return haveToGetComponent ? spawned.GetComponent<T>() : null;
        }

        public T Spawn<T>(T prefab, Transform parent, bool worldPositionStays = false)
            where T : Component
        {
            GetPositionAndRotationByParent(
                prefab.gameObject,
                parent,
                out var position,
                out var rotation
            );
            var spawned = DefaultSpawn(
                prefab.gameObject,
                position,
                rotation,
                parent,
                worldPositionStays,
                out var haveToGetComponent
            );
            return haveToGetComponent ? spawned.GetComponent<T>() : null;
        }

        /// <summary>
        /// * despawn the clone
        /// </summary>
        /// <param name="clone"></param>
        /// <param name="delay"></param>
        public void Despawn(Component clone, float delay = 0f)
        {
            DefaultDespawn(clone.gameObject, delay);
        }

        /// <summary>
        /// * despawn the clone
        /// </summary>
        /// <param name="clone"></param>
        /// <param name="delay"></param>
        public void Despawn(UnityEngine.GameObject clone, float delay = 0f)
        {
            DefaultDespawn(clone, delay);
        }

        /// <summary>
        /// * Fire an action for each pool
        /// </summary>
        /// <param name="action"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void ForEachPool(Action<GameObjectPool> action)
        {
#if DEBUG
            if (action == null)
                throw new ArgumentNullException(nameof(action));
#endif
            foreach (var pool in _allPoolsMap.Values)
                action.Fire(pool);
        }

        /// <summary>
        /// * Fire an action for each clone.
        /// </summary>
        /// <param name="action"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void ForEachClone(Action<UnityEngine.GameObject> action)
        {
#if DEBUG
            if (action == null)
                throw new ArgumentNullException(nameof(action));
#endif
            foreach (var poolable in ClonesMap.Values)
                action.Fire(poolable.GameObject);
        }

        /// <summary>
        /// * Get pool by prefab name
        /// </summary>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        public GameObjectPool GetPoolByPrefabName(string prefabName)
        {
            foreach (
                var poolKey in _allPoolsMap.Keys.Where(poolKey => poolKey.name.Equals(prefabName))
            )
                return _allPoolsMap[poolKey];
            Log.Log.MsgE($"未通过预制体名称 '{prefabName}' 找到池!");
            return null;
        }

        /// <summary>
        /// * Try get pool by prefab component
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="pool"></param>
        /// <returns></returns>
        public bool TryGetPoolByPrefab(Component prefab, out GameObjectPool pool)
        {
            return TryGetPoolByPrefab(prefab.gameObject, out pool);
        }

        /// <summary>
        /// * Try to get pool by game object prefab
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="pool"></param>
        /// <returns></returns>
        public bool TryGetPoolByPrefab(UnityEngine.GameObject prefab, out GameObjectPool pool)
        {
            return _allPoolsMap.TryGetValue(prefab, out pool);
        }

        public bool TryGetPoolByClone(Component clone, out GameObjectPool pool)
        {
            return TryGetPoolByClone(clone.gameObject, out pool);
        }

        public bool TryGetPoolByClone(UnityEngine.GameObject clone, out GameObjectPool pool)
        {
            if (ClonesMap.TryGetValue(clone, out var poolable) && poolable.IsSetup)
            {
                pool = poolable.Pool;
                return true;
            }

            pool = null;
            return false;
        }

        public GameObjectPool GetPoolByClone(UnityEngine.GameObject clone)
        {
            var hasPool = TryGetPoolByClone(clone, out var pool);
#if DEBUG
            if (!hasPool)
                Log.Log.MsgE($"克隆 '{clone}' 未找到对应的池!", clone);
#endif
            return pool;
        }

        public GameObjectPool GetPoolByClone(Component clone)
        {
            return GetPoolByClone(clone.gameObject);
        }

        public GameObjectPool GetPoolByPrefab(UnityEngine.GameObject prefab)
        {
            var hasPool = TryGetPoolByPrefab(prefab, out var pool);
#if DEBUG
            if (!hasPool)
                Log.Log.MsgE($"未通过预制体 '{prefab}' 找到池!", prefab);
#endif
            return pool;
        }

        public GameObjectPool GetPoolByPrefab(Component prefab)
        {
            return GetPoolByPrefab(prefab.gameObject);
        }

        /// <summary>
        /// * Is the game object a clone (spawned using pool)
        /// </summary>
        /// <param name="clone"></param>
        /// <returns></returns>
        public bool IsClone(UnityEngine.GameObject clone)
        {
            return ClonesMap.ContainsKey(clone);
        }

        public bool IsClone(Component clone)
        {
            return IsClone(clone.gameObject);
        }

        public PoolableStatus GetCloneStatus(UnityEngine.GameObject clone)
        {
            if (ClonesMap.TryGetValue(clone.gameObject, out var poolable))
                return poolable.Status;
#if DEBUG
            Log.Log.MsgE($"克隆 '{clone}' 不是可池化的!", clone);
#endif
            return default;
        }

        public PoolableStatus GetCloneStatus(Component clone)
        {
            return GetCloneStatus(clone.gameObject);
        }

        /// <summary>
        /// Destroys a clone.
        /// </summary>
        /// <param name="clone">Component which spawned via F8Pool</param>
        public void DestroyClone(Component clone)
        {
            DestroyPoolableWithGameObject(clone.gameObject, false);
        }

        /// <summary>
        /// Destroys a clone.
        /// </summary>
        /// <param name="clone">GameObject which spawned via F8Pool</param>
        public void DestroyClone(UnityEngine.GameObject clone)
        {
            DestroyPoolableWithGameObject(clone, false);
        }

        /// <summary>
        /// Destroys a clone immediately.
        /// </summary>
        /// <param name="clone">GameObject which spawned via F8Pool</param>
        public void DestroyCloneImmediately(Component clone)
        {
            DestroyPoolableWithGameObject(clone.gameObject, true);
        }

        /// <summary>
        /// Destroys a clone immediately.
        /// </summary>
        /// <param name="clone">GameObject which spawned via F8Pool</param>
        public void DestroyCloneImmediately(UnityEngine.GameObject clone)
        {
            DestroyPoolableWithGameObject(clone, true);
        }

        /// <summary>
        /// Destroys all pools.
        /// </summary>
        /// <param name="immediately">Should all pools be destroyed immediately?</param>
        public void DestroyAllPools(bool immediately = false)
        {
            if (!CanFirePoolAction())
            {
#if DEBUG
                Log.Log.MsgE("在应用程序退出时，您正在尝试销毁所有池！");
#endif
                return;
            }

            if (immediately)
                ForEachPool(x => x.DestroyPoolImmediately());
            else
                ForEachPool(x => x.DestroyPool());
        }

        #endregion

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroyRelease() { }

        public void OnGui() { }
    }
}
