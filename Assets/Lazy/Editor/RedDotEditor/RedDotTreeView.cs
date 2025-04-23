using Lazy.RedDot;
using Unity.VisualScripting;
using UnityEditor.IMGUI.Controls;

namespace Lazy.Editor.RedDotEditor
{
    public class RedDotTreeView : TreeView
    {
        /// <summary>
        /// * 根节点
        /// </summary>
        private RedDotTreeViewItem _root;

        private int _id;

        public RedDotTreeView(TreeViewState state)
            : base(state)
        {
            Reload();
            useScrollView = true;

            RedDotManager.Instance.NodeCountChangeCallback += Reload;
            RedDotManager.Instance.NodeValueChangeCallback += Repaint;
        }

        protected override TreeViewItem BuildRoot()
        {
            _id = 0;
            _root = PreOrder(RedDotManager.Instance.Root);
            _root.depth = -1;

            SetupDepthsFromParentsAndChildren(_root);

            return _root;
        }

        private RedDotTreeViewItem PreOrder(TrieNode root)
        {
            if (root == null)
                return null;

            var item = new RedDotTreeViewItem(_id++, root);
            if (root.ChildrenCount > 0)
                foreach (var child in root.Children)
                    item.AddChild(PreOrder(child));

            return item;
        }

        private void Repaint(TrieNode node, int value)
        {
            Repaint();
        }

        public void OnDestroy()
        {
            RedDotManager.Instance.NodeCountChangeCallback -= Reload;
            RedDotManager.Instance.NodeValueChangeCallback -= Repaint;
        }
    }
}
