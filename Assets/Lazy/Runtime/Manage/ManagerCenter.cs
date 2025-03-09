using UnityEngine;

namespace Lazy.Manage
{
    public class ManagerCenter
    {
        /// <summary>
        /// * 根MonoBehaviour
        /// </summary>
        private static MonoBehaviour _behaviour;

        private class ManagerWrapper
        {
            /// <summary>
            /// * 优先级
            /// </summary>
            public int priority = 0;

            public IManager manager;

            /// <summary>
            /// * 准备移除
            /// </summary>
            public bool readyToBeRemoved = false;
        }
    }
}