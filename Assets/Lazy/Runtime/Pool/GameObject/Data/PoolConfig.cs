using System;
using UnityEngine;

namespace Lazy
{
    /// <summary>
    /// * 一个GameObjectPool的配置
    /// </summary>
    [Serializable]
    public class PoolConfig
    {
#if UNITY_EDITOR
        [Tooltip(PoolConstant.PoolName)]
        [SerializeField]
        private string poolName;

        [Space]
#endif
        [Tooltip(PoolConstant.PoolEnabled)]
        [SerializeField]
        private bool enabled = true;

        [Tooltip("预制体")]
        [SerializeField]
        private GameObject prefab;

        [Tooltip(PoolConstant.CapacityReachedBehaviourDesc)]
        [SerializeField]
        private CapacityReachedBehaviour capacityReachedBehaviour =
            PoolConstant.DefaultCapacityReachedBehaviour;

        [Tooltip(PoolConstant.DespawnTypeDesc)]
        [SerializeField]
        private DespawnType despawnType = PoolConstant.DefaultDespawnType;

        [Tooltip(PoolConstant.CallbackTypeDesc)]
        [SerializeField]
        private CallbackType callbackType = PoolConstant.DefaultCallbackType;

        [Tooltip(PoolConstant.CapacityDesc)]
        [Min(0)]
        [SerializeField]
        private int capacity = PoolConstant.DefaultCapacity;

        [Tooltip(PoolConstant.PreloadSizeDesc)]
        [Min(0)]
        [SerializeField]
        private int preloadSize = PoolConstant.DefaultPreloadSize;

        [Tooltip(PoolConstant.PersistentDesc)]
        [SerializeField]
        private bool dontDestroyOnLoad = true;

        [Tooltip(PoolConstant.WarningDesc)]
        [SerializeField]
        private bool warning = true;

        public bool Enabled => enabled;
        public GameObject Prefab => prefab;
        public CapacityReachedBehaviour CapacityReachedBehaviour => capacityReachedBehaviour;
        public DespawnType DespawnType => despawnType;
        public CallbackType CallbackType => callbackType;
        public int Capacity => capacity;
        public int PreloadSize => preloadSize;
        public bool DontDestroyOnLoad => dontDestroyOnLoad;
        public bool Warning => warning;
    }
}
