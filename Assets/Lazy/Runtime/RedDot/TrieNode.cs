using System;
using System.Collections.Generic;

namespace Lazy.RedDot
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
        private Action<int> _onCountChanged;

        /// <summary>
        /// * 当前前缀名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// * 节点值
        /// </summary>
        public int Value { get; private set; }

        // TODO:
    }
}
