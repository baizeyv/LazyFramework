using Lazy;
using UnityEngine;

namespace Lazy
{
    /// <summary>
    /// * 可池化的GameObject对象
    /// </summary>
    public class GameObjectPoolable
    {
        /// <summary>
        /// * 对应的对象池
        /// </summary>
        internal GameObjectPool Pool;

        /// <summary>
        /// * Transform
        /// </summary>
        internal Transform Transform;

        /// <summary>
        /// * instantiated game object
        /// </summary>
        internal GameObject GameObject;

        internal PoolableStatus Status;

        internal bool IsSetup;

        internal void SetupAsDefault()
        {
#if DEBUG
            if (IsSetup)
                Log.Log.MsgE("池对象已经设置！");
#endif
            GameObjectPoolManager.Instance.ClonesMap.Add(GameObject, this);
            Status = PoolableStatus.Despawned;
            IsSetup = true;
        }

        internal void SetupAsSpawnedOverCapacity()
        {
#if DEBUG
            if (IsSetup)
                Log.Log.MsgE("池对象已经设置！");
#endif
            GameObjectPoolManager.Instance.ClonesMap.Add(GameObject, this);
            Status = PoolableStatus.SpawnedOverCapacity;
            IsSetup = true;
        }

        internal void Dispose(bool immediately)
        {
            GameObjectPoolManager.Instance.ClonesMap.Remove(GameObject);
            if (immediately)
                Object.DestroyImmediate(GameObject);
            else
                Object.Destroy(GameObject);
        }
    }
}
