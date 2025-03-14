using System;
using System.Collections.Generic;
using Lazy.Event;
using Lazy.Pool.Attribute;
using Lazy.Pool.GameObject.Enums;
using Lazy.Utility;
using UnityEditor;
using UnityEngine;

namespace Lazy.Pool.GameObject
{
    [DisallowMultipleComponent]
    public class GameObjectPool : MonoBehaviour
    {
        [Header("MAIN")]
        [Tooltip("当前池预制体")]
        [SerializeField]
        internal UnityEngine.GameObject prefab;

        [Tooltip(PoolConstant.CapacityReachedBehaviourDesc)]
        [SerializeField]
        internal CapacityReachedBehaviour capacityReachedBehaviour =
            PoolConstant.DefaultCapacityReachedBehaviour;

        [Tooltip(PoolConstant.DespawnTypeDesc)]
        [SerializeField]
        internal DespawnType despawnType = PoolConstant.DefaultDespawnType;

        [Tooltip("容量")]
        [SerializeField]
        [Delayed]
        [Min(0)]
        private int capacity = PoolConstant.DefaultCapacity;

        [Header("Preload")]
        [Tooltip("该池子的克隆预加载类型")]
        [SerializeField]
        private PreloadType preloadType = PreloadType.Disabled;

        [Tooltip("预加载大小")]
        [SerializeField]
        [Delayed]
        [Min(0)]
        private int preloadSize = PoolConstant.DefaultPreloadSize;

        [Header("Callback")]
        [Tooltip(PoolConstant.CallbackTypeDesc)]
        [SerializeField]
        internal CallbackType callbackType = PoolConstant.DefaultCallbackType;

        [Header("Persistent")]
        [Tooltip("此池是否应该是持久的")]
        [SerializeField]
        internal bool dontDestroyOnLoad = true;

        [Header("Debug")]
        [Tooltip("此池是否应该查找问题并记录警告？")]
        [SerializeField]
        internal bool sendWarnings = true;

        [Space]
        [Tooltip("所有克隆的数量")]
        [ReadOnlyInspectorField]
        [SerializeField]
        private int allClonesCount;

        [Tooltip("已经取出的克隆数量")]
        [ReadOnlyInspectorField]
        [SerializeField]
        private int spawnedClonesCount;

        [Tooltip("已经放回的克隆数量")]
        [ReadOnlyInspectorField]
        [SerializeField]
        private int despawnedClonesCount;

        [Tooltip("取出次数")]
        [ReadOnlyInspectorField]
        [SerializeField]
        private int spawnsCount;

        [Tooltip("放回次数")]
        [ReadOnlyInspectorField]
        [SerializeField]
        private int despawnsCount;

        [Tooltip("总数量")]
        [ReadOnlyInspectorField]
        [SerializeField]
        private int total;

        [Tooltip("实例化数量")]
        [ReadOnlyInspectorField]
        [SerializeField]
        private int instantiated;

        [HideInInspector]
        [SerializeField]
        private List<UnityEngine.GameObject> gameObjectsToPreload;

        [ReadOnlyInspectorField]
        [SerializeField]
        private bool hasPreloadedGameObjects;

        internal Transform _cachedTransform;

        internal Vector3 _regularPrefabScale;

        internal bool _isSetup;

        private readonly PoolList<GameObjectPoolable> _spawnedPoolables =
            new(PoolConstant.DefaultPoolableListCapacity);

        private readonly PoolList<GameObjectPoolable> _despawnedPoolables =
            new(PoolConstant.DefaultPoolableListCapacity);

        private PoolList<GameObjectPoolable> _temporaryPoolables;

        private Transform _prefabTransform;

#if UNITY_EDITOR
        private UnityEngine.GameObject _cachedPrefab;
#endif

        /// <summary>
        /// * The prefab attached to this pool.
        /// </summary>
        public UnityEngine.GameObject AttachedPrefab => prefab;

        /// <summary>
        /// * Pool overflow behaviour
        /// </summary>
        public CapacityReachedBehaviour CapacityReachedBehaviour => capacityReachedBehaviour;

        /// <summary>
        /// * Clone despawn type
        /// </summary>
        public DespawnType DespawnType => despawnType;

        /// <summary>
        /// * Callback on clone spawn and despawn
        /// </summary>
        public CallbackType CallbackType => callbackType;

        /// <summary>
        /// * Pool capacity
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// * Number of spawned clones
        /// </summary>
        public int SpawnedClonesCount => spawnedClonesCount;

        /// <summary>
        /// * Number of despawned clones
        /// </summary>
        public int DespawnedClonesCount => despawnedClonesCount;

        /// <summary>
        /// * Number of all clones
        /// </summary>
        public int AllClonesCount => allClonesCount;

        /// <summary>
        /// * Number of spawns
        /// </summary>
        public int SpawnsCount => spawnsCount;

        /// <summary>
        /// * Number of despawns
        /// </summary>
        public int DespawnsCount => despawnsCount;

        /// <summary>
        /// * Number of instantiates
        /// </summary>
        public int InstantiatesCount => instantiated;

        /// <summary>
        /// * Total number of spawns and despawns
        /// </summary>
        public int TotalCount => total;

        /// <summary>
        /// * Has this pool registered as persistent
        /// </summary>
        public bool HasRegisteredAsPersistent =>
            GameObjectPoolManager.Instance.HasPoolRegisteredAsPersistent(this);

        /// <summary>
        /// * The actions will be performed on a game object spawned by this pool.
        /// </summary>
        public readonly SimpleEvent<UnityEngine.GameObject> GameObjectSpawnedEvent = new();

        /// <summary>
        /// * The actions will be performed on a game object despawned by this pool.
        /// </summary>
        public readonly SimpleEvent<UnityEngine.GameObject> GameObjectDespawnedEvent = new();

        /// <summary>
        /// * The actions will be performed on a game object instantiated by this pool.
        /// </summary>
        public readonly SimpleEvent<UnityEngine.GameObject> GameObjectInstantiatedEvent = new();

        #region Life Cycle

        private void Awake()
        {
            if (prefab == null)
                return;
            if (dontDestroyOnLoad && HasRegisteredAsPersistent)
            {
                DestroyPool();
                return;
            }

            if (TrySetup(prefab))
                PreloadElements(PreloadType.OnAwake);
        }

        private void Start()
        {
            if (!_isSetup)
                return;
            PreloadElements(PreloadType.OnStart);
            FirePreloadedClonesAndClear();
        }

        #endregion

        #region Manunal Initialize

        /// <summary>
        /// * You can initialize the pool manually using this method
        /// </summary>
        public void Init()
        {
            Init(prefab);
        }

        /// <summary>
        /// * You can initialize the pool manually using this method
        /// </summary>
        /// <param name="prefab"></param>
        public void Init(UnityEngine.GameObject prefab)
        {
#if DEBUG
            if (_isSetup)
                if (sendWarnings)
                    Log.Log.MsgD("池已经初始化完毕！", this);

            if (prefab == null)
            {
                Log.Log.MsgE("您正在尝试使用空预制体初始化此池！", this);
                return;
            }

            if (hasPreloadedGameObjects && prefab != this.prefab)
            {
                Log.Log.MsgE(
                    "此池已预加载游戏对象，而您正在尝试使用另一个预制体初始化此池！"
                        + "清除此池或使用正确的预制体进行初始化。",
                    this
                );
                return;
            }
#endif
            if (TrySetup(this.prefab))
                FirePreloadedClonesAndClear();
        }

        #endregion

        /// <summary>
        /// * Try to setup current pool
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        internal bool TrySetup(UnityEngine.GameObject prefab)
        {
            if (_isSetup)
                return false;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Log.Log.MsgE("应用程序未运行时，无法设置池！", this);
                return false;
            }

            if (GameObjectPoolManager.Instance.checkForPrefab)
                if (!CheckForPrefab(prefab))
                    return false;

            _cachedPrefab = prefab;
#endif
            this.prefab = prefab;
            _cachedTransform = transform;
            _prefabTransform = prefab.transform;
            _regularPrefabScale = _prefabTransform.localScale;

            if (dontDestroyOnLoad)
                if (!TryRegisterPoolAsPersistent())
                    return false;

            if (hasPreloadedGameObjects)
                SetupPreloadedClones();
            GameObjectPoolManager.Instance.RegisterPool(this);
            _isSetup = true;
            return true;
        }

        internal void UnRegisterPoolable(GameObjectPoolable poolable)
        {
            RemovePoolableUnorderedFromList(_spawnedPoolables, poolable, ref spawnedClonesCount);
            RemovePoolableUnorderedFromList(
                _despawnedPoolables,
                poolable,
                ref despawnedClonesCount
            );
        }

        /// <summary>
        /// * 获取一个 Poolable
        /// </summary>
        /// <param name="arguments"></param>
        internal void Get(out GetPoolableArgument arguments)
        {
            if (_despawnedPoolables.Count <= 0)
            {
                // # 没有已经放回的GameObject
                if (allClonesCount >= capacity)
                {
                    // # 已经达到最大容量了
                    if (capacityReachedBehaviour == CapacityReachedBehaviour.Recycle)
                    {
                        // # 回收已经取出的第一个
                        var poolable = _spawnedPoolables.Components[0];
                        _spawnedPoolables.RemoveAt(0);
                        _spawnedPoolables.Add(poolable);
                        arguments = new GetPoolableArgument(poolable, false);
                        return;
                    }

                    if (
                        capacityReachedBehaviour == CapacityReachedBehaviour.InstantiateWithCallback
                    )
                    {
                        InstantiatePoolableOverCapacity(out arguments);
                        return;
                    }

                    if (capacityReachedBehaviour == CapacityReachedBehaviour.Instantiate)
                        InstantiatePoolableOverCapacity(out arguments);

                    if (capacityReachedBehaviour == CapacityReachedBehaviour.ReturnNullableClone)
                    {
                        arguments = new GetPoolableArgument(null, true);
                        return;
                    }

                    if (capacityReachedBehaviour == CapacityReachedBehaviour.ThrowException)
                    {
#if DEBUG
                        Log.Log.MsgE("已达到容量上限！无法生成新的克隆！", this);
#endif
                        arguments = new GetPoolableArgument(null, true);
                        return;
                    }
                }

                arguments = new GetPoolableArgument(InstantiateAndSetupPoolable(false), false);
                AddPoolableToList(_spawnedPoolables, arguments.Poolable, ref spawnedClonesCount);
                return;
            }

            arguments = new GetPoolableArgument(_despawnedPoolables.Components[0], false);
            // # 将第一个已回收的放入到取出列表
            AddPoolableToList(
                _spawnedPoolables,
                _despawnedPoolables.Components[0],
                ref spawnedClonesCount
            );
            // # 移除第一个已回收的
            RemoveFirstPoolableUnordered(_despawnedPoolables, ref despawnedClonesCount);
        }

        private void SetupPreloadedClones()
        {
            for (var i = 0; i < gameObjectsToPreload.Count; i++)
            {
                var clone = gameObjectsToPreload[i];
#if DEBUG
                if (clone == null)
                {
                    Log.Log.MsgE(
                        $"其中一个预加载的游戏对象已被销毁！在应用程序未运行时，从组件上下文菜单清除 '{name}' 池 "
                            + "以解决此问题！",
                        this
                    );
                    continue;
                }
#endif
                SetupPoolableAsDefault(clone, out var poolable);
                AddPoolableToList(_despawnedPoolables, poolable, ref despawnedClonesCount);
            }
        }

        private GameObjectPoolable InstantiateAndSetupPoolable(bool isPopulatingPool)
        {
            var newGameObject = Instantiate(prefab);
            SetupPoolableAsDefault(newGameObject, out var poolable);

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(newGameObject);

            if (isPopulatingPool)
            {
                poolable.GameObject.SetVisible(false);
                poolable.Transform.SetParent(_isSetup ? _cachedTransform : transform, false);
            }

            GameObjectPoolManager.Instance.GameObjectInstantiated.Fire(newGameObject);
            FireGameObjectInstantiatedCallback(newGameObject);
            return poolable;
        }

        private void InstantiatePoolableOverCapacity(out GetPoolableArgument arguments)
        {
            var newGameObject = Instantiate(prefab);
            SetupPoolableAsSpawnedOverCapacity(newGameObject, out var poolable);
            arguments = new GetPoolableArgument(poolable, false);
            FireGameObjectInstantiatedCallback(poolable.GameObject);
        }

        private void AddPoolableToList(
            PoolList<GameObjectPoolable> poolList,
            GameObjectPoolable poolable,
            ref int count
        )
        {
            poolList.Add(poolable);
            count++;
            allClonesCount++;
        }

        private void RemovePoolableUnorderedFromList(
            PoolList<GameObjectPoolable> list,
            GameObjectPoolable poolable,
            ref int count
        )
        {
            for (var i = 0; i < list.Count; i++)
                if (list.Components[i] == poolable)
                {
                    list.RemoveUnorderedAt(i);
                    count--;
                    allClonesCount--;
                    return;
                }
        }

        private void RemoveFirstPoolableUnordered(PoolList<GameObjectPoolable> list, ref int count)
        {
            list.RemoveUnorderedAt(0);
            count--;
            allClonesCount--;
        }

        private void SetupPoolableAsDefault(
            UnityEngine.GameObject clone,
            out GameObjectPoolable poolable
        )
        {
            poolable = CreatePoolable(clone);
            poolable.SetupAsDefault();
        }

        private void SetupPoolableAsSpawnedOverCapacity(
            UnityEngine.GameObject clone,
            out GameObjectPoolable poolable
        )
        {
            poolable = CreatePoolable(clone);
            poolable.SetupAsSpawnedOverCapacity();
        }

        private GameObjectPoolable CreatePoolable(UnityEngine.GameObject clone)
        {
            return new GameObjectPoolable()
            {
                Pool = this,
                GameObject = clone,
                Transform = clone.transform,
            };
        }

        /// <summary>
        /// * Try register current pool as a persistent pool
        /// </summary>
        /// <returns></returns>
        private bool TryRegisterPoolAsPersistent()
        {
            if (!HasRegisteredAsPersistent)
            {
#if DEBUG
                if (_cachedTransform.parent != null)
                {
                    Log.Log.MsgE(
                        "池不能是持久的！"
                            + "因为此 GameObject 有父 Transform，"
                            + "而 DontDestroyOnLoad 只对根 GameObject 或根 GameObject 上的组件有效。",
                        this
                    );
                    return false;
                }
#endif
                dontDestroyOnLoad = true;
                DontDestroyOnLoad(gameObject);
                GameObjectPoolManager.Instance.RegisterPersistentPool(this);
                return true;
            }

            DestroyPool();
            return false;
        }

        private void PreloadElements(PreloadType requiredType)
        {
            if (preloadType != requiredType)
                return;
            if (allClonesCount >= capacity)
                return;
            PopulatePool(preloadSize);
        }

        /// <summary>
        /// * Populates this pool (填充池)
        /// </summary>
        /// <param name="count">populate count</param>
        public void PopulatePool(int count)
        {
#if DEBUG
            if (!_isSetup)
            {
                Log.Log.MsgE($"池 '{name}' 未设置！", this);
                return;
            }

            if (!Application.isPlaying)
            {
                Log.Log.MsgE($"在应用程序未运行时，您正在尝试填充池 '{name}'！", this);
                return;
            }

            if (count < 0)
            {
                Log.Log.MsgE("填充数量不能小于零！", this);
                return;
            }
#endif
            for (var i = 0; i < count; i++)
            {
                if (allClonesCount >= capacity)
                {
#if DEBUG
                    if (sendWarnings)
                        Log.Log.MsgD($"池 {name} 达到最大容量！");
#endif
                    return;
                }

                preloadSize = Mathf.Clamp(count, 0, capacity);
                AddPoolableToList(
                    _despawnedPoolables,
                    InstantiateAndSetupPoolable(true),
                    ref despawnedClonesCount
                );
            }
        }

        internal void FireGameObjectSpawnedCallback(UnityEngine.GameObject obj)
        {
            FirePoolActionCallback(obj, ref spawnsCount, GameObjectSpawnedEvent);
        }

        internal void FireGameObjectDespawnedCallback(UnityEngine.GameObject obj)
        {
            FirePoolActionCallback(obj, ref despawnsCount, GameObjectDespawnedEvent);
        }

        private void FireGameObjectInstantiatedCallback(UnityEngine.GameObject obj)
        {
            instantiated++;
            GameObjectInstantiatedEvent.Fire(obj);
        }

        private void FirePreloadedClonesAndClear()
        {
            if (hasPreloadedGameObjects)
            {
                for (var i = 0; i < gameObjectsToPreload.Count; i++)
                    GameObjectInstantiatedEvent.Fire(gameObjectsToPreload[i]);

                hasPreloadedGameObjects = false;
                gameObjectsToPreload = null;
            }
        }

        private void FirePoolActionCallback(
            UnityEngine.GameObject clone,
            ref int actionCount,
            SimpleEvent<UnityEngine.GameObject> poolEvent
        )
        {
            total++;
            actionCount++;
            poolEvent.Fire(clone);
        }

        private void HidePoolable(GameObjectPoolable poolable)
        {
            poolable.Transform.SetParent(_cachedTransform, true);
        }

        private void SetPoolableParentAsNull(GameObjectPoolable poolable)
        {
            poolable.Transform.SetParent(null, false);
        }

        internal void Free(GameObjectPoolable poolable)
        {
            if (poolable.Status == PoolableStatus.Despawned)
            {
#if DEBUG
                if (sendWarnings)
                    Log.Log.MsgD(
                        $"池对象 '{poolable.GameObject}' 已经取消生成！",
                        poolable.GameObject
                    );
#endif
                return;
            }

            poolable.GameObject.SetVisible(false);
            switch (despawnType)
            {
                case DespawnType.DeactivateAndHide:
                    HidePoolable(poolable);
                    break;
                case DespawnType.DeactivateAndSetNullParent:
                    SetPoolableParentAsNull(poolable);
                    break;
                case DespawnType.OnlyDeactivate:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(despawnType));
            }

            // # Add to despawned list
            AddPoolableToList(_despawnedPoolables, poolable, ref despawnedClonesCount);
            // # Remove from spawned list
            RemovePoolableUnorderedFromList(_spawnedPoolables, poolable, ref spawnedClonesCount);
        }

        /// <summary>
        /// Set the capacity of this pool
        /// </summary>
        /// <param name="capacity"></param>
        public void SetCapacity(int capacity)
        {
#if DEBUG
            if (capacity < 0)
            {
                Log.Log.MsgE($"池 '{name}' 的容量不能小于零！", this);
                return;
            }

            if (capacity < allClonesCount)
            {
                Log.Log.MsgE($"池 '{name}' 的容量不能小于所有克隆的数量！", this);
                return;
            }

            if (hasPreloadedGameObjects && this.capacity < gameObjectsToPreload.Count)
            {
                Log.Log.MsgE($"池 '{name}' 的容量不能小于预加载克隆的数量！", this);
                return;
            }

            if (sendWarnings && capacity == 0)
                Log.Log.MsgD($"池 '{name}' 的容量为零。", this);
#endif
            this.capacity = capacity;
            preloadSize = Mathf.Clamp(preloadSize, 0, this.capacity);
        }

        /// <summary>
        /// * Set the capacity reached behaviour of this pool
        /// </summary>
        /// <param name="behaviour"></param>
        public void SetCapacityReachedBehaviour(CapacityReachedBehaviour behaviour)
        {
            capacityReachedBehaviour = behaviour;
        }

        /// <summary>
        /// * Set the despawn type of this pool
        /// </summary>
        /// <param name="type"></param>
        public void SetDespawnType(DespawnType type)
        {
            despawnType = type;
        }

        /// <summary>
        /// * Set the callback type of this pool
        /// </summary>
        /// <param name="type"></param>
        public void SetCallbackType(CallbackType type)
        {
            callbackType = type;
        }

        /// <summary>
        /// * Set the warnings active of this pool
        /// </summary>
        /// <param name="flag"></param>
        public void SetWarning(bool flag)
        {
            sendWarnings = flag;
        }

        /// <summary>
        /// * Fire an action for each clone
        /// </summary>
        /// <param name="action"></param>
        public void ForEachClone(Action<UnityEngine.GameObject> action)
        {
            ForEachSpawnedClone(action);
            ForEachDespawnedClone(action);
        }

        /// <summary>
        /// * Fire an action for each spawned clone
        /// </summary>
        /// <param name="action"></param>
        public void ForEachSpawnedClone(Action<UnityEngine.GameObject> action)
        {
            ForEach(_spawnedPoolables, action);
        }

        /// <summary>
        /// * Fire an action for each despawned clone
        /// </summary>
        /// <param name="action"></param>
        public void ForEachDespawnedClone(Action<UnityEngine.GameObject> action)
        {
            ForEach(_despawnedPoolables, action);
        }

        /// <summary>
        /// * Destroys this pool with clones
        /// </summary>
        public void DestroyPool()
        {
            Clear();
            GameObjectPoolManager.Instance.UnregisterPool(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// * Destroy this pool with clones immediately
        /// </summary>
        public void DestroyPoolImmediately()
        {
            Clear();
            GameObjectPoolManager.Instance.UnregisterPool(this);
            DestroyImmediate(gameObject);
        }

        /// <summary>
        /// * Destroys all clones in this pool (also destroys preloaded clones).
        /// </summary>
#if UNITY_EDITOR
        [ContextMenu("Clear")]
#endif
        public void Clear()
        {
            ClearEvents();
            ClearGameObjectsToPreload();
            DestroyAllClonesImmediately();
            ResetCounts();
        }

        /// <summary>
        /// * Destroy all clones in this pool
        /// </summary>
        public void DestroyAllClones()
        {
            DestroySpawnedClones();
            DestroyDespawnedClones();
        }

        /// <summary>
        /// * Destroy spawned clones in this pool
        /// </summary>
        public void DestroySpawnedClones()
        {
            DisposePoolablesInList(_spawnedPoolables, ref spawnedClonesCount, false);
        }

        /// <summary>
        /// * Destroy despawned clones in this pool
        /// </summary>
        public void DestroyDespawnedClones()
        {
            DisposePoolablesInList(_despawnedPoolables, ref despawnedClonesCount, false);
        }

        /// <summary>
        /// * Destroys all clones in this pool
        /// </summary>
        public void DestroyAllClonesImmediately()
        {
            DestroySpawnedClonesImmediately();
            DestroyDespawnedClonesImmediately();
        }

        /// <summary>
        /// * Destroy spawned clones object immediately
        /// </summary>
        public void DestroySpawnedClonesImmediately()
        {
            DisposePoolablesInList(_spawnedPoolables, ref spawnedClonesCount, true);
        }

        /// <summary>
        /// * Destroy despawned clones object immediately
        /// </summary>
        public void DestroyDespawnedClonesImmediately()
        {
            DisposePoolablesInList(_despawnedPoolables, ref despawnedClonesCount, true);
        }

        /// <summary>
        /// * Despawn all spawned clones
        /// </summary>
        public void DespawnAllClones()
        {
            _temporaryPoolables ??= new PoolList<GameObjectPoolable>(
                PoolConstant.DefaultPoolableListCapacity
            );
            for (var i = 0; i < _spawnedPoolables.Count; i++)
                _temporaryPoolables.Add(_spawnedPoolables.Components[i]);

            for (var i = 0; i < _temporaryPoolables.Count; i++)
                GameObjectPoolManager.Instance.DespawnImmediately(
                    _temporaryPoolables.Components[i]
                );

            if (_temporaryPoolables.Count > 0)
            {
                _temporaryPoolables.Clear();
                _temporaryPoolables.SetCapacity(PoolConstant.DefaultPoolableListCapacity);
            }
        }

        private void DisposePoolablesInList(
            PoolList<GameObjectPoolable> list,
            ref int count,
            bool immediately
        )
        {
            for (var i = 0; i < list.Count; i++)
            {
                list.Components[i].Dispose(immediately);
                allClonesCount--;
            }

            list.Clear();
            count--;
        }

        private static void ForEach(
            PoolList<GameObjectPoolable> list,
            Action<UnityEngine.GameObject> action
        )
        {
            if (action == null)
                return;
            for (var i = 0; i < list.Count; i++)
                action.Fire(list.Components[i].GameObject);
        }

        /// <summary>
        /// * Clear all game objects which is ready to preload
        /// </summary>
        private void ClearGameObjectsToPreload()
        {
            if (gameObjectsToPreload == null)
                return;
            foreach (var item in gameObjectsToPreload)
                DestroyImmediate(item);
            gameObjectsToPreload.Clear();
            hasPreloadedGameObjects = false;
        }

        private void ClearEvents()
        {
            GameObjectSpawnedEvent.Dispose();
            GameObjectDespawnedEvent.Dispose();
            GameObjectInstantiatedEvent.Dispose();
        }

        private void ResetCounts()
        {
            allClonesCount = 0;
            instantiated = 0;
            spawnsCount = 0;
            despawnsCount = 0;
            total = 0;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ClampCapacity();
            ClampPreloadSize();
            CheckPreloadedClonesForErrors();
            CheckForPrefabMatchOnPlay();
            CheckForPrefab(prefab);
        }

        private void ClampCapacity()
        {
            if (hasPreloadedGameObjects && capacity < gameObjectsToPreload.Count)
            {
                Log.Log.MsgE("容量不能小于预加载克隆的数量！", this);
                capacity = gameObjectsToPreload.Count;
            }

            if (_despawnedPoolables != null)
                if (capacity < allClonesCount)
                {
                    Log.Log.MsgE("容量不能小于所有克隆的数量！", this);
                    capacity = allClonesCount;
                }
        }

        private void ClampPreloadSize()
        {
            if (preloadSize > capacity)
                preloadSize = capacity;
        }

        private void CheckPreloadedClonesForErrors()
        {
            if (hasPreloadedGameObjects)
            {
                if (prefab == null)
                    Log.Log.MsgE(
                        "此池中已预加载游戏对象，但现在预制体为空！"
                            + "设置正确的预制体以解决此问题或清除此池。",
                        this
                    );
                for (var i = 0; i < gameObjectsToPreload.Count; i++)
                {
                    var clone = gameObjectsToPreload[i];
                    if (clone == null)
                    {
                        Log.Log.MsgE(
                            "此池的预加载游戏对象之一为空！" + "清除此池以解决此问题。",
                            this
                        );
                        return;
                    }

                    if (
                        !Application.isPlaying
                        && PrefabUtility.GetCorrespondingObjectFromSource(clone) != prefab
                    )
                    {
                        Log.Log.MsgE(
                            "您预加载的游戏对象与预制体不匹配。" + "清除此池或设置正确的预制体。",
                            this
                        );
                        return;
                    }
                }
            }
        }

        private void CheckForPrefabMatchOnPlay()
        {
            if (_isSetup && Application.isPlaying)
                if (_cachedPrefab != null && prefab != _cachedPrefab)
                    prefab = _cachedPrefab;
        }

        private bool CheckForPrefab(UnityEngine.GameObject gameObjectToCheck)
        {
            if (gameObjectToCheck == null)
                return false;
            if (gameObjectToCheck.scene.isLoaded)
            {
                Log.Log.MsgE("您不能将场景中的游戏对象设置为预制体！", this);
                prefab = null;
                return false;
            }

            if (PrefabUtility.IsPartOfAnyPrefab(gameObjectToCheck))
            {
                Log.Log.MsgE($"'{gameObjectToCheck}' 不是一个预制体！", this);
                prefab = null;
                return false;
            }

            return true;
        }

        [ContextMenu("Preload")]
        private void Preload()
        {
            for (var i = 0; i < preloadSize; i++)
                if (!TryPreloadGameObject())
                    return;
        }

        [ContextMenu("Preload One")]
        private void PreloadOne()
        {
            TryPreloadGameObject();
        }

        private bool TryPreloadGameObject()
        {
            if (CanPreloadGameObject())
            {
                PreloadGameObject();
                return true;
            }

            return false;
        }

        private bool CanPreloadGameObject()
        {
            if (prefab == null)
            {
                Log.Log.MsgE($"池 '{name}' 的预制体为空！", this);
                return false;
            }

            if (!CheckForPrefab(prefab))
                return false;

            if (gameObjectsToPreload.Count > capacity || allClonesCount >= capacity)
            {
                if (sendWarnings)
                    Log.Log.MsgD("已达到容量上限！无法预加载更多游戏对象！", this);
                return false;
            }

            return true;
        }

        private void PreloadGameObject()
        {
            var obj = PrefabUtility.InstantiatePrefab(prefab, transform) as UnityEngine.GameObject;
            if (obj == null)
                return;
            obj.SetVisible(false);
            instantiated++;
            if (Application.isPlaying)
            {
                SetupPoolableAsDefault(obj, out var poolable);
                AddPoolableToList(_despawnedPoolables, poolable, ref despawnedClonesCount);
            }
            else
            {
                hasPreloadedGameObjects = true;
                gameObjectsToPreload.Add(obj);
            }
        }
#endif
    }
}
