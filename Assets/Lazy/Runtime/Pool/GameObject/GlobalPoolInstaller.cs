using System;
using Lazy;
using Lazy.Pool;
using Lazy.Singleton;
using UnityEngine;

namespace Lazy
{
    /// <summary>
    /// * 全局的GameObjectPool, 对于没有预设的池子,使用这个池子的参数
    /// ! 这个并不是真正的池子,而是所有池子的安装器 (通过这个MonoBehaviour来安装预设的池子)
    /// </summary>
    [MonoSingletonPath("Lazy/Pool/GlobalGameObjectPool")]
    [DisallowMultipleComponent]
    [ManagerUpdate]
    [ManagerFixedUpdate]
    [ManagerLateUpdate]
    public sealed class GlobalPoolInstaller : MonoSingleton<GlobalPoolInstaller>, IManager
    {
        [Header("Main")]
        [Tooltip(PoolConstant.GlobalPoolUpdateTypeDesc)]
        [SerializeField]
        private UpdateType updateType = UpdateType.Update;

        [Tooltip(PoolConstant.GlobalPreloadTypeDesc)]
        [SerializeField]
        private PreloadType preloadPoolsType = PreloadType.Disabled;

        [Tooltip("Pools Config")]
        [SerializeField]
        private PoolsConfig poolsConfig;

        [Tooltip(PoolConstant.CapacityReachedBehaviourDesc)]
        [SerializeField]
        internal CapacityReachedBehaviour capacityReachedBehaviour =
            PoolConstant.DefaultCapacityReachedBehaviour;

        [Tooltip(PoolConstant.DespawnTypeDesc)]
        [SerializeField]
        internal DespawnType despawnType = PoolConstant.DefaultDespawnType;

        [Tooltip(PoolConstant.CallbackTypeDesc)]
        [SerializeField]
        internal CallbackType callbackType = PoolConstant.DefaultCallbackType;

        [Tooltip(PoolConstant.CapacityDesc)]
        [Min(0)]
        [SerializeField]
        internal int capacity = PoolConstant.DefaultCapacity;

        [Tooltip(PoolConstant.PersistentDesc)]
        [SerializeField]
        internal bool dontDestroyOnLoad = true;

        [Tooltip(PoolConstant.WarningDesc)]
        [SerializeField]
        internal bool warning = true;

        [Header("Safety")]
        [Tooltip(PoolConstant.PoolModeDesc)]
        [SerializeField]
        internal PoolMode poolMode = PoolConstant.DefaultPoolMode;

        [Tooltip(PoolConstant.DelayedDespawnReactionDesc)]
        [SerializeField]
        internal ReactionOnRepeatedDelayedDespawn reactionOnRepeatedDelayedDespawn =
            PoolConstant.DefaultDelayedDespawnHandleType;

        [Tooltip(PoolConstant.DespawnPersistentClonesOnDestroyDesc)]
        [SerializeField]
        private bool despawnPersistentClonesOnDestroy = true;

        [Tooltip(PoolConstant.CheckClonesForNullDesc)]
        [SerializeField]
        private bool checkClonesForNull = true;

        [Tooltip(PoolConstant.CheckForPrefabDesc)]
        [SerializeField]
        private bool checkForPrefab = false;

        [Tooltip(PoolConstant.ClearEventsOnDestroyDesc)]
        [SerializeField]
        private bool clearEventsOnDestroy;

        private GlobalPoolInstaller() { }

        private void Initialize()
        {
#if DEBUG
            if (
                GameObjectPoolManager.Instance.installer != null
                && GameObjectPoolManager.Instance.installer != this
            )
                Log.Log.MsgE($"场景中的 {nameof(GameObjectPoolManager)} 实例数量大于一个！");

            if (!enabled)
                Log.Log.MsgD(
                    $"<{nameof(GlobalPoolInstaller)}> 实例已禁用！"
                        + "因此，某些功能可能无法正常工作！",
                    this
                );
#endif
            GameObjectPoolManager.Instance.isApplicationQuitting = false;
            GameObjectPoolManager.Instance.installer = this;
            GameObjectPoolManager.Instance.hasPoolInitialized = true;
            GameObjectPoolManager.Instance.poolMode = poolMode;
            GameObjectPoolManager.Instance.checkForPrefab = checkForPrefab;
            GameObjectPoolManager.Instance.checkClonesForNull = checkClonesForNull;
            GameObjectPoolManager.Instance.despawnPersistentClonesOnDestroy =
                despawnPersistentClonesOnDestroy;
        }

        private void PreloadPools(PreloadType type)
        {
            if (type != preloadPoolsType)
                return;
            GameObjectPoolManager.Instance.InstallPools(poolsConfig);
        }

        private void HandleDespawnRequests(float deltaTime)
        {
            for (var i = 0; i < GameObjectPoolManager.Instance.DespawnRequests.Count; i++)
            {
                ref var request = ref GameObjectPoolManager.Instance.DespawnRequests.Components[i];
                if (request.Poolable.Status == PoolableStatus.Despawned)
                {
                    GameObjectPoolManager.Instance.DespawnRequests.RemoveUnorderedAt(i);
                    continue;
                }

                request.TimeToDespawn -= deltaTime;
                if (request.TimeToDespawn <= 0f)
                {
                    GameObjectPoolManager.Instance.DespawnImmediately(request.Poolable);
                    GameObjectPoolManager.Instance.DespawnRequests.RemoveUnorderedAt(i);
                }
            }
        }

        private void Start()
        {
            PreloadPools(PreloadType.OnStart);
        }

        public override void OnSingletonInitialize()
        {
            AppLauncher.Instance.OnQuitGame += () =>
            {
                GameObjectPoolManager.Instance.isApplicationQuitting = true;
            };
            Initialize();
            PreloadPools(PreloadType.OnAwake);
        }

        public void OnUpdate()
        {
            if (updateType == UpdateType.Update)
                HandleDespawnRequests(Time.deltaTime);
        }

        public void OnFixedUpdate()
        {
            if (updateType == UpdateType.FixedUpdate)
                HandleDespawnRequests(Time.deltaTime);
        }

        public void OnLateUpdate()
        {
            if (updateType == UpdateType.LateUpdate)
                HandleDespawnRequests(Time.deltaTime);
        }

        public void OnDestroyRelease()
        {
            GameObjectPoolManager.Instance.ResetPool();
            if (clearEventsOnDestroy || GameObjectPoolManager.Instance.isApplicationQuitting)
                GameObjectPoolManager.Instance.GameObjectInstantiated.Dispose();
            Destroy(gameObject);
        }

        public void OnGui() { }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                GameObjectPoolManager.Instance.poolMode = poolMode;
                GameObjectPoolManager.Instance.checkForPrefab = checkForPrefab;
                GameObjectPoolManager.Instance.checkClonesForNull = checkClonesForNull;
                GameObjectPoolManager.Instance.despawnPersistentClonesOnDestroy =
                    despawnPersistentClonesOnDestroy;
            }
        }
#endif
    }
}
