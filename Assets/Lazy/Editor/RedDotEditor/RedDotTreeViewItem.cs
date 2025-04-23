using Lazy.RedDot;
using UnityEditor.IMGUI.Controls;

namespace Lazy.Editor.RedDotEditor
{
    public class RedDotTreeViewItem : TreeViewItem
    {
        private TrieNode _node;

        /// <summary>
        /// * 节点路径
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// * 节点值
        /// </summary>
        public int Value { get; private set; }

        public RedDotTreeViewItem(int id, TrieNode node)
        {
            base.id = id;
            _node = node;
            Path = node.FullPath;
            Value = node.Value;
        }

        public override string displayName =>
            $"{_node.Name} - 节点值: {_node.Value} - 子节点数: {_node.ChildrenCount}";
    }
}
