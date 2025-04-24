using System.Collections.Generic;
using System.Linq;

namespace Lazy
{
    /// <summary>
    /// * 用于统一管理Update的管理器
    /// </summary>
    [ManagerUpdate]
    [ManagerFixedUpdate]
    [ManagerLateUpdate]
    public class MonoNodeManager : Singleton<MonoNodeManager>, IManager
    {
        /// <summary>
        /// * 所有的 Mono 节点
        /// </summary>
        private static readonly Dictionary<MonoNode, NodeWrapper> AllNodes = new(100);

        /// <summary>
        /// * 使用 Update() 的节点列表
        /// </summary>
        private static readonly List<NodeWrapper> UpdateNodes = new(100);

        /// <summary>
        /// * 使用 LateUpdate() 的节点列表
        /// </summary>
        private static readonly List<NodeWrapper> LateUpdateNodes = new(100);

        /// <summary>
        /// * 使用 FixedUpdate() 的节点列表
        /// </summary>
        private static readonly List<NodeWrapper> FixedUpdateNodes = new(100);

        private MonoNodeManager() { }

        public void AddNode(MonoNode node, int priority = 0)
        {
            if (priority < 0)
            {
                Log.MsgW("Priority can not is negative. Auto switch to 0");
                priority = 0;
            }

            var wrapper = new NodeWrapper(node, priority);
            if (AllNodes.TryGetValue(node, out var nd))
            {
                // # 已经添加了
                if (nd.priority == priority)
                    // # 优先级相同
                    return;
                AllNodes.Remove(node);
                if (node.AllowUpdate())
                    UpdateNodes.Remove(nd);
                if (node.AllowFixedUpdate())
                    FixedUpdateNodes.Remove(nd);
                if (node.AllowLateUpdate())
                    LateUpdateNodes.Remove(nd);
            }

            if (priority == 0)
            {
                // # 没有设置优先级
                var maxPriority = GetMaxPriority();
                priority = ++maxPriority;
            }

            wrapper.priority = priority;
            AllNodes[node] = wrapper;

            if (node.AllowUpdate())
            {
                UpdateNodes.Add(wrapper);
                UpdateNodes.Sort(
                    (left, right) =>
                    {
                        if (left.priority < right.priority)
                            return -1;
                        return left.priority > right.priority ? 1 : 0;
                    }
                );
            }

            if (node.AllowFixedUpdate())
            {
                FixedUpdateNodes.Add(wrapper);
                FixedUpdateNodes.Sort(
                    (left, right) =>
                    {
                        if (left.priority < right.priority)
                            return -1;
                        return left.priority > right.priority ? 1 : 0;
                    }
                );
            }

            if (node.AllowLateUpdate())
            {
                LateUpdateNodes.Add(wrapper);
                LateUpdateNodes.Sort(
                    (left, right) =>
                    {
                        if (left.priority < right.priority)
                            return -1;
                        return left.priority > right.priority ? 1 : 0;
                    }
                );
            }
        }

        public void RemoveNode(MonoNode node)
        {
            if (AllNodes.TryGetValue(node, out var nd))
            {
                AllNodes.Remove(node);
                if (node.AllowUpdate())
                    UpdateNodes.Remove(nd);
                if (node.AllowFixedUpdate())
                    FixedUpdateNodes.Remove(nd);
                if (node.AllowLateUpdate())
                    LateUpdateNodes.Remove(nd);
            }
        }

        /// <summary>
        /// * 移除所有Mono节点
        /// </summary>
        public void RemoveAllNode()
        {
            AllNodes.Clear();
            UpdateNodes.Clear();
            LateUpdateNodes.Clear();
            FixedUpdateNodes.Clear();
        }

        /// <summary>
        /// * 获取当前最大的优先级
        /// </summary>
        /// <returns></returns>
        private static int GetMaxPriority()
        {
            return AllNodes.Values.Select(item => item.priority).Prepend(int.MinValue).Max();
        }

        public void OnUpdate()
        {
            for (var i = 0; i < UpdateNodes.Count; i++)
            {
                if (UpdateNodes[i]?.node == null || UpdateNodes[i].readyToBeRemoved)
                {
                    UpdateNodes.RemoveAt(i);
                    i--;
                    continue;
                }

                UpdateNodes[i].node.Process();
            }
        }

        public void OnFixedUpdate()
        {
            for (var i = 0; i < FixedUpdateNodes.Count; i++)
            {
                if (FixedUpdateNodes[i]?.node == null || FixedUpdateNodes[i].readyToBeRemoved)
                {
                    FixedUpdateNodes.RemoveAt(i);
                    i--;
                    continue;
                }

                FixedUpdateNodes[i].node.PhysicsProcess();
            }
        }

        public void OnLateUpdate()
        {
            for (var i = 0; i < LateUpdateNodes.Count; i++)
            {
                if (LateUpdateNodes[i]?.node == null || LateUpdateNodes[i].readyToBeRemoved)
                {
                    LateUpdateNodes.RemoveAt(i);
                    i--;
                    continue;
                }

                LateUpdateNodes[i].node.LateProcess();
            }
        }

        public void OnDestroyRelease() { }

        public void OnGui() { }

        private class NodeWrapper
        {
            /// <summary>
            /// * 优先级
            /// </summary>
            public int priority = 0;

            /// <summary>
            /// * Mono 节点
            /// </summary>
            public MonoNode node;

            /// <summary>
            /// * 准备移除的标志
            /// </summary>
            public bool readyToBeRemoved = false;

            public NodeWrapper(MonoNode node, int priority)
            {
                this.node = node;
                this.priority = priority;
            }

            public override bool Equals(object obj)
            {
                if (obj is NodeWrapper that)
                    return that.node == node;

                return false;
            }

            public override int GetHashCode()
            {
                return node?.GetHashCode() ?? 0;
            }
        }
    }
}
