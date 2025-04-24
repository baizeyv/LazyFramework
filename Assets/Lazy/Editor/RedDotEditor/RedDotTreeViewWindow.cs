using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LazyEditor
{
    public class RedDotTreeViewWindow : EditorWindow
    {
        private static RedDotTreeViewWindow _window;

        private RedDotTreeView _treeView;

        private SearchField _searchField;

        [MenuItem("Lazy/RedDot Editor")]
        private static void OpenWindow()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("警告", "运行后才能打开红点树视图窗口", "了解");
                return;
            }

            _window = GetWindow<RedDotTreeViewWindow>();
            _window.titleContent = new GUIContent("RedDot");
            _window.Show();
        }

        private void OnEnable()
        {
            _treeView = new RedDotTreeView(new TreeViewState());
            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;

            EditorApplication.playModeStateChanged += OnPlayModeStateChange;
        }

        private void OnPlayModeStateChange(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    _window.Close();
                    break;
            }
        }

        private void OnDestroy()
        {
            _treeView.OnDestroy();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChange;
        }

        private void OnGUI()
        {
            UpToolbar();

            TreeView();

            BottomToolBar();
        }

        private void UpToolbar()
        {
            _treeView.searchString = _searchField.OnGUI(
                new Rect(0, 0, position.width - 40f, 20f),
                _treeView.searchString
            );
        }

        private void TreeView()
        {
            _treeView.OnGUI(new Rect(0, 20f, position.width, position.height - 40f));
        }

        private void BottomToolBar()
        {
            GUILayout.BeginArea(new Rect(20f, position.height - 18f, position.width - 40f, 16f));

            using (new EditorGUILayout.HorizontalScope())
            {
                var style = "miniButton";
                if (GUILayout.Button("展开全部节点", style))
                    _treeView.ExpandAll();

                if (GUILayout.Button("收起全部节点", style))
                    _treeView.CollapseAll();
            }

            GUILayout.EndArea();
        }
    }
}
