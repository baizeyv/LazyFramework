using System;
using System.Collections.Generic;
using System.Text;
using Lazy.Manage;
using Lazy.Singleton;

namespace Lazy.RedDot
{
    [ManagerUpdate]
    public class RedDotManager : Singleton<RedDotManager>, IManager
    {
        /// <summary>
        /// * 所有节点字典集合
        /// </summary>
        private Dictionary<string, TrieNode> _allNodes;

        /// <summary>
        /// * 脏节点集合
        /// </summary>
        private HashSet<TrieNode> _dirtyNodes;

        /// <summary>
        /// * 临时脏节点列表
        /// </summary>
        private List<TrieNode> _tempDirtyNodes;

        /// <summary>
        /// * 节点数量改变的回调 (主要用于Editor界面的刷新Repaint)
        /// </summary>
        public Action NodeCountChangeCallback;

        /// <summary>
        /// * 节点值改变的回调 (主要用于Editor界面的刷新Repaint)
        /// </summary>
        public Action<TrieNode, int> NodeValueChangeCallback;

        /// <summary>
        /// * 路径分隔符
        /// </summary>
        public char SplitChar { get; private set; }

        /// <summary>
        /// * 缓存的StringBuilder
        /// </summary>
        public StringBuilder Cached { get; private set; }

        /// <summary>
        /// * 红点树根节点
        /// </summary>
        public TrieNode Root { get; private set; }

        private RedDotManager() { }

        public override void OnSingletonInitialize()
        {
            SplitChar = '/';
            _allNodes = new Dictionary<string, TrieNode>();
            Root = new TrieNode("root");
            _dirtyNodes = new HashSet<TrieNode>();
            _tempDirtyNodes = new List<TrieNode>();
            Cached = new StringBuilder();
        }

        /// <summary>
        /// * 添加节点的值改变监听
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public TrieNode AddListener(string path, Action<int> callback)
        {
            if (callback == null)
                return null;
            var node = GetTrieNode(path);
            node.AddListener(callback);
            return node;
        }

        /// <summary>
        /// * 移除值改变监听器
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        public void RemoveListener(string path, Action<int> callback)
        {
            if (callback == null)
                return;
            var node = GetTrieNode(path);
            node.RemoveListener(callback);
        }

        /// <summary>
        /// * 移除所有节点值监听
        /// </summary>
        /// <param name="path"></param>
        public void RemoveAllListener(string path)
        {
            var node = GetTrieNode(path);
            node.RemoveAllListener();
        }

        /// <summary>
        /// * 设置叶子节点值
        /// ! 不是叶子节点将会报错!
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newValue"></param>
        public void SetValue(string path, int newValue)
        {
            var node = GetTrieNode(path);
            node.SetValue(newValue);
        }

        /// <summary>
        /// * 获取节点值
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public int GetValue(string path)
        {
            var node = GetTrieNode(path);
            if (node == null)
                return 0;
            return node.Value;
        }

        /// <summary>
        /// * 根据前缀路径获取对应节点
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public TrieNode GetTrieNode(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Log.MsgE("路径不合法，不能为空");
                return null;
            }

            if (_allNodes.TryGetValue(path, out var node))
                return node;

            var cur = Root;
            var length = path.Length;
            var startIndex = 0;

            for (var i = 0; i < length; i++)
                if (path[i] == SplitChar)
                {
                    // # 找到分隔符了
                    if (i == length - 1)
                    {
                        Log.Log.MsgE("路径不合法，不能以路径分隔符结尾：" + path);
                        return null;
                    }

                    var endIndex = i - 1;
                    if (endIndex < startIndex)
                    {
                        Log.Log.MsgE(
                            "路径不合法，不能存在连续的路径分隔符或以路径分隔符开头：" + path
                        );
                        return null;
                    }

                    var child = cur.GetOrAddChild(new RangeString(path, startIndex, endIndex));
                    // # 更新startIndex
                    startIndex = i + 1;
                    cur = child;
                }

            // # 最后一个节点,直接用length - 1 作为 endIndex
            var target = cur.GetOrAddChild(new RangeString(path, startIndex, length - 1));
            _allNodes.Add(path, target);
            return target;
        }

        /// <summary>
        /// * 移除指定节点
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool RemoveTrieNode(string path)
        {
            if (!_allNodes.ContainsKey(path))
                return false;
            var node = GetTrieNode(path);
            _allNodes.Remove(path);
            return node.Parent.RemoveChild(new RangeString(node.Name, 0, node.Name.Length - 1));
        }

        /// <summary>
        /// * 移除所有节点
        /// </summary>
        public void RemoveAllTrieNode()
        {
            Root.RemoveAllChild();
            _allNodes.Clear();
        }

        /// <summary>
        /// * 标记需要更新的脏节点
        /// </summary>
        /// <param name="node"></param>
        public void MarkDirtyNode(TrieNode node)
        {
            if (node == null || node.Name == Root.Name)
                return;
            _dirtyNodes.Add(node);
        }

        public void OnUpdate()
        {
            if (_dirtyNodes.Count == 0)
                return;
            _tempDirtyNodes.Clear();
            foreach (var dirtyNode in _dirtyNodes)
                _tempDirtyNodes.Add(dirtyNode);
            _dirtyNodes.Clear();
            // # 处理所有脏节点
            foreach (var node in _tempDirtyNodes)
                node.SetValue();
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroyRelease() { }

        public void OnGui() { }
    }
}
