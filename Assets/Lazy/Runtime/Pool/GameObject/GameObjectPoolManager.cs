using System;
using System.Collections.Generic;
using System.Linq;
using Lazy.Event;
using Lazy.Pool.GameObject.Data;
using Lazy.Pool.GameObject.Enums;
using Lazy.Utility;
using Unity.VisualScripting;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lazy.Pool.GameObject
{
    public class GameObjectPoolManager : Singleton.Singleton<GameObjectPoolManager>
    {
        internal GlobalGameObjectPool installer = null;

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
            // TODO:
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
            // TODO:
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
                            $"<{nameof(GlobalGameObjectPool)}> 实例已自动创建。也可以手动添加以修改默认参数。"
                        );
#endif
                    }

                hasPoolInitialized = true;
            }
        }

        private void CreateInstallerInstance()
        {
            installer = GlobalGameObjectPool.Instance;
        }

        /// <summary>
        /// * 尝试获取对象池安装器
        /// </summary>
        /// <param name="installer"></param>
        /// <returns></returns>
        private bool TryFindPoolInstallerInstanceAsSingle(out GlobalGameObjectPool installer)
        {
            var ins = Object.FindObjectsOfType<GlobalGameObjectPool>();
            var length = ins.Length;
            if (length > 0)
            {
#if DEBUG
                if (length > 1)
                {
                    for (var i = 1; i < length; i++)
                        Object.Destroy(ins[i]);
                    Log.Log.MsgE($"场景中 {nameof(GlobalGameObjectPool)} 实例的数量大于一个！");
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
                    Log.Log.MsgE($"<{nameof(GlobalGameObjectPool)}> 实例为空！");
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

        #endregion
    }
}
