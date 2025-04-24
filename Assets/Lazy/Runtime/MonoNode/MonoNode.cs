using UnityEngine;

namespace Lazy
{
    public abstract class MonoNode : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            MonoNodeManager.Instance.AddNode(this, Priority());
        }

        protected virtual void OnDisable()
        {
            MonoNodeManager.Instance.RemoveNode(this);
        }

        /// <summary>
        /// * 自定义 Update()
        /// </summary>
        internal abstract void Process();

        /// <summary>
        /// * 自定义 FixedUpdate();
        /// </summary>
        internal abstract void PhysicsProcess();

        /// <summary>
        /// * 自定义 LateUpdate();
        /// </summary>
        internal abstract void LateProcess();

        /// <summary>
        /// * 是否允许使用Update()
        /// </summary>
        /// <returns></returns>
        internal abstract bool AllowUpdate();

        /// <summary>
        /// * 是否允许使用FixedUpdate()
        /// </summary>
        /// <returns></returns>
        internal abstract bool AllowFixedUpdate();

        /// <summary>
        /// * 是否允许使用LateUpdate()
        /// </summary>
        /// <returns></returns>
        internal abstract bool AllowLateUpdate();

        /// <summary>
        /// * 获取优先级
        /// </summary>
        /// <returns></returns>
        internal abstract int Priority();
    }
}
