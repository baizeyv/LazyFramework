using Lazy.Editor.AssetEditor;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor
{
    public static class Helper
    {
        [MenuItem("Lazy/打包AssetBundles目录资源 _F1")]
        public static void BuildAssetBundles()
        {
            AssetBundleBuildTool.BuildAllAssetBundles();
        }

        [MenuItem("Lazy/Editor Mode")]
        public static void SwitchIsEditorMode()
        {
            var isEditorMode = EditorPrefs.GetBool(EditorConstant.IsEditorModeKey, false);
            EditorPrefs.SetBool(EditorConstant.IsEditorModeKey, !isEditorMode);
        }

        [MenuItem("Lazy/Editor Mode", true)]
        public static bool SetIsEditorMode()
        {
            var isEditorMode = EditorPrefs.GetBool(EditorConstant.IsEditorModeKey, false);
            Menu.SetChecked("Lazy/Editor Mode", isEditorMode);
            return true;
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethods()
        {
            // 在Project面板按空格键相当于Show In Explorer
            EditorApplication.projectWindowItemOnGUI += ProjectWindowItemOnGUI;
            GenCodeSetting.Instance.Save();
        }

        private static void ProjectWindowItemOnGUI(string guid, Rect selectionRect)
        {
            if (
                UnityEngine.Event.current.type == EventType.KeyDown
                && UnityEngine.Event.current.keyCode == KeyCode.Space
                && selectionRect.Contains(UnityEngine.Event.current.mousePosition)
            )
            {
                EditorApplication.delayCall += () =>
                {
                    var strPath = AssetDatabase.GUIDToAssetPath(guid);

                    EditorUtility.RevealInFinder(strPath);

                    var obj = AssetDatabase.LoadAssetAtPath<Object>(strPath);
                    if (obj != null)
                        EditorGUIUtility.PingObject(obj);
                };
                UnityEngine.Event.current.Use();
            }
        }
    }
}
