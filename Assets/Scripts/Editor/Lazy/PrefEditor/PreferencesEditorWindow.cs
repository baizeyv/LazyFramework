using System;
using UnityEditor;
using UnityEngine;

namespace Editor.Lazy.PrefEditor
{
    public class PreferencesEditorWindow : EditorWindow
    {
        private const string WindowName = "Preferences Editor";

        /// <summary>
        /// * 本地存储路径
        /// </summary>
        private static string pathToPrefs = string.Empty;

        /// <summary>
        /// * 存储路径前缀 (不同平台的前缀不同)
        /// </summary>
        private static string platformPathPrefix = "~";

        [MenuItem("Lazy/Preferences Editor", false, 100)]
        public static void ShowWindow()
        {
            if (HasOpenInstances<PreferencesEditorWindow>())
            {
                // # 如果已经打开了就关闭
                GetWindow<PreferencesEditorWindow>(WindowName).Close();
            }
            else
            {
                var window = GetWindow<PreferencesEditorWindow>(WindowName);
                window.minSize = new Vector2(270f, 300f);
                window.name = WindowName;
                window.Show();
            }
        }

        private void OnEnable()
        {
#if UNITY_EDITOR_WIN
            pathToPrefs = @"SOFTWARE\Unity\UnityEditor\" + PlayerSettings.companyName + @"\" +
                          PlayerSettings.productName;
            platformPathPrefix = "<CurrentUser>";
#endif
            // TODO:
        }
    }
}