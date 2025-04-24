using System;
using System.Collections.Generic;
using Lazy;

namespace Lazy
{
    /// <summary>
    /// * 前缀树节点
    /// </summary>
    public class TrieNode
    {
        /// <summary>
        /// * 子节点字典
        /// </summary>
        private Dictionary<RangeString, TrieNode> _children;

        /// <summary>
        /// * 完整路径
        /// </summary>
        private string _fullPath;

        /// <summary>
        /// * 节点值改变回调
        /// </summary>
        private event Action<int> OnCountChanged;

        /// <summary>
        /// * 当前前缀名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// * 父节点
        /// </summary>
        public TrieNode Parent;

        /// <summary>
        /// * 完整路径
        /// </summary>
        public string FullPath
        {
            get
            {
                if (string.IsNullOrEmpty(_fullPath))
                {
                    if (Parent == null || Parent == RedDotManager.Instance.Root)
                        _fullPath = Name;
                    else
                        _fullPath = Parent.FullPath + RedDotManager.Instance.SplitChar + Name;
                }

                return _fullPath;
            }
        }

        /// <summary>
        /// * 节点值
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        /// * 子节点集合
        /// </summary>
        public Dictionary<RangeString, TrieNode>.ValueCollection Children => _children?.Values;

        /// <summary>
        /// * 子节点数量
        /// </summary>
        public int ChildrenCount
        {
            get
            {
                if (_children == null)
                    return 0;
                var sum = _children.Count;
                foreach (var node in Children)
                    sum += node.ChildrenCount;

                return sum;
            }
        }

        public TrieNode(string name)
        {
            Name = name;
            Value = 0;
            OnCountChanged = null;
        }

        public TrieNode(string name, TrieNode parent)
            : this(name)
        {
            Parent = parent;
        }

        #region Listener

        /// <summary>
        /// * 添加值改变监听器
        /// </summary>
        /// <param name="callback"></param>
        public void AddListener(Action<int> callback)
        {
            OnCountChanged += callback;
        }

        /// <summary>
        /// * 移除指定的值改变监听器
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveListener(Action<int> callback)
        {
            OnCountChanged -= callback;
        }

        /// <summary>
        /// * 移除所有值改变监听器
        /// </summary>
        public void RemoveAllListener()
        {
            OnCountChanged = null;
        }

        #endregion

        /// <summary>
        /// * 改变节点值
        /// !(使用传入的新值,只能在叶子节点上调用)
        /// </summary>
        /// <param name="newValue"></param>
        public void SetValue(int newValue)
        {
            if (_children != null && _children.Count != 0)
            {
                Log.MsgE("不允许直接改变非叶子节点的值：" + FullPath);
                return;
            }

            InternalChangeValue(newValue);
        }

        /// <summary>
        /// * 改变节点值
        /// !(根据子节点的值计算新值,只能在非叶子节点上使用)
        /// </summary>
        public void SetValue()
        {
            var sum = 0;
            if (_children != null && _children.Count != 0)
                foreach (var child in _children)
                    sum += child.Value.Value;
            InternalChangeValue(sum);
        }

        private void InternalChangeValue(int newValue)
        {
            if (Value == newValue)
                return;
            Value = newValue;
            OnCountChanged.Fire(newValue);

            RedDotManager.Instance.NodeValueChangeCallback.Fire(this, Value);
            // # 标记父节点为脏节点
            RedDotManager.Instance.MarkDirtyNode(Parent);
        }

        /// <summary>
        /// * 获取子节点,如若不存在则添加
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TrieNode GetOrAddChild(RangeString key)
        {
            var child = GetChild(key);
            if (child == null)
                child = AddChild(key);

            return child;
        }

        /// <summary>
        /// * 获取子节点
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TrieNode GetChild(RangeString key)
        {
            if (_children == null || _children.Count == 0)
                return null;
            return _children.GetValueOrDefault(key);
        }

        public TrieNode AddChild(RangeString key)
        {
            if (_children == null)
            {
                _children = new Dictionary<RangeString, TrieNode>();
            }
            else if (_children.ContainsKey(key))
            {
                Log.MsgE("子节点添加失败，不允许重复添加：" + FullPath);
                return _children[key];
            }

            var child = new TrieNode(key.ToString(), this);
            _children.Add(key, child);
            RedDotManager.Instance.NodeCountChangeCallback.Fire();
            return child;
        }

        /// <summary>
        /// * 移除子节点
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool RemoveChild(RangeString key)
        {
            if (_children == null || _children.Count == 0)
                return false;
            var child = GetChild(key);
            if (child != null)
            {
                // # 子节点被删除 需要进行一次父节点的刷新
                RedDotManager.Instance.MarkDirtyNode(this);
                _children.Remove(key);
                RedDotManager.Instance.NodeCountChangeCallback.Fire();
                return true;
            }

            return false;
        }

        /// <summary>
        /// * 移除所有子节点
        /// </summary>
        public void RemoveAllChild()
        {
            if (_children == null || _children.Count == 0)
                return;
            _children.Clear();
            RedDotManager.Instance.MarkDirtyNode(this);
            RedDotManager.Instance.NodeCountChangeCallback.Fire();
        }

        public override string ToString()
        {
            return FullPath;
        }
    }
}
