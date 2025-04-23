using System;
using System.IO;
using System.Linq;
using Lazy;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lazy.Editor.UIEditor
{
    [CustomEditor(typeof(ViewPresenter), true)]
    public class ViewPresenterInspector : UnityEditor.Editor
    {
        [MenuItem("GameObject/Lazy/Add ViewPresenter(Alt+2) &2", false, 0)]
        public static void AddView()
        {
            var gameObject = Selection.objects.First() as GameObject;
            if (!gameObject)
            {
                Log.Log.MsgE("需要选择GameObject");
                return;
            }

            gameObject.GetOrAddComponent<ViewPresenter>();
        }

        [MenuItem("GameObject/Lazy/Add Bind(Alt+1) &1", false, 1)]
        public static void AddBind()
        {
            foreach (var o in Selection.objects.OfType<GameObject>())
            {
                if (!o)
                    continue;

                o.GetOrAddComponent<Bind>();
                EditorUtility.SetDirty(o);
                EditorSceneManager.MarkSceneDirty(o.scene);
            }
        }

        /// <summary>
        /// * 当前游戏中存在的IApp类型列表
        /// </summary>
        private Type[] _appTypes;

        private string[] _appTypeMenus;

        private Type[] _viewPresenterTypes;

        private string[] _viewPresenterTypeMenus;

        private ViewPresenter ViewPresenter => target as ViewPresenter;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(ViewPresenter.scriptsFolder))
            {
                var setting = GenCodeSetting.Instance;
                ViewPresenter.scriptsFolder = setting.scriptDirectory;
            }

            if (string.IsNullOrEmpty(ViewPresenter.prefabFolder))
            {
                var setting = GenCodeSetting.Instance;
                ViewPresenter.prefabFolder = setting.prefabDirectory;
            }

            if (string.IsNullOrEmpty(ViewPresenter.scriptName))
                ViewPresenter.scriptName = ViewPresenter.name;

            if (string.IsNullOrEmpty(ViewPresenter.nameSpace))
            {
                var setting = GenCodeSetting.Instance;
                ViewPresenter.nameSpace = setting.nameSpace;
            }

            _appTypes = FindAllIAppTypes();
            _appTypeMenus = _appTypes.Select(x => x.FullName).Append("None").ToArray();
            _viewPresenterTypes = FindAllViewPresenterTypes();
            _viewPresenterTypeMenus = _viewPresenterTypes
                .Select(x => x.FullName)
                .Append("Lazy.ViewPresenter")
                .ToArray();
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical("HelpBox");
            {
                GUILayout.Label("<color=#7bed9f>Source</color>", Styles.BigTitleStyle.Value);
                DrawPropertiesExcluding(serializedObject, "m_Script");
                serializedObject.ApplyModifiedProperties();
            }
            GUILayout.EndVertical();
            GUILayout.BeginVertical("HelpBox");
            {
                GUILayout.Label("<color=#ecb0c1>Generation</color>", Styles.BigTitleStyle.Value);
                if (_appTypes != null && _appTypes.Length > 0)
                {
                    var index = Array.FindIndex(
                        _appTypes,
                        t => t.FullName.Equals(ViewPresenter.appFullTypeName)
                    );
                    if (index == -1)
                        // # 没有找到
                        index = _appTypeMenus.Length - 1;
                    GUILayout.BeginHorizontal("box");
                    {
                        GUILayout.Label("IApp:", GUILayout.Width(150));
                        EditorGUI.BeginChangeCheck();
                        index = EditorGUILayout.Popup(index, _appTypeMenus);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (index == _appTypeMenus.Length - 1)
                                ViewPresenter.appFullTypeName = string.Empty;
                            else
                                ViewPresenter.appFullTypeName = _appTypes[index].FullName;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                if (_viewPresenterTypes != null && _viewPresenterTypes.Length > 0)
                {
                    var index = Array.FindIndex(
                        _viewPresenterTypes,
                        x => x.FullName.Equals(ViewPresenter.viewPresenterFullTypeName)
                    );
                    if (index == -1)
                        index = _viewPresenterTypeMenus.Length - 1;
                    GUILayout.BeginHorizontal("box");
                    {
                        GUILayout.Label("Presenter", GUILayout.Width(150));
                        EditorGUI.BeginChangeCheck();
                        index = EditorGUILayout.Popup(index, _viewPresenterTypeMenus);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (index == _viewPresenterTypeMenus.Length - 1)
                                ViewPresenter.viewPresenterFullTypeName = string.Empty;
                            else
                                ViewPresenter.viewPresenterFullTypeName = _viewPresenterTypes[
                                    index
                                ].FullName;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal("box");
                {
                    GUILayout.Label("Namespace", GUILayout.Width(150));
                    ViewPresenter.nameSpace = EditorGUILayout.TextArea(ViewPresenter.nameSpace);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal("box");
                {
                    GUILayout.Label("ScriptName", GUILayout.Width(150));
                    ViewPresenter.scriptName = EditorGUILayout.TextArea(ViewPresenter.scriptName);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginVertical("helpbox");
                {
                    GUILayout.Label("<color=#ffbe76>Script</color>", Styles.BigTitleStyle.Value);
                    GUILayout.BeginHorizontal("box");
                    {
                        GUILayout.Label("ScriptFolder", GUILayout.Width(150));
                        EditorGUILayout.TextArea(ViewPresenter.scriptsFolder);
                        if (GUILayout.Button("...", GUILayout.Width(30)))
                        {
                            var folderPath = Application.dataPath.Replace(
                                "Assets",
                                ViewPresenter.scriptsFolder
                            );
                            folderPath = EditorUtility.OpenFolderPanel(
                                "Select Folder",
                                folderPath,
                                string.Empty
                            );
                            ViewPresenter.scriptsFolder = folderPath.Replace(
                                Application.dataPath,
                                "Assets"
                            );
                        }
                    }
                    GUILayout.EndHorizontal();

                    EditorGUILayout.LabelField("Drag or write script folder");
                    var pathRect = EditorGUILayout.GetControlRect();
                    pathRect.height = 35;
                    GUI.Box(pathRect, string.Empty);
                    EditorGUILayout.LabelField(string.Empty);
                    if (
                        (
                            UnityEngine.Event.current.type == EventType.DragUpdated
                            || UnityEngine.Event.current.type == EventType.DragPerform
                        ) && pathRect.Contains(UnityEngine.Event.current.mousePosition)
                    )
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        if (UnityEngine.Event.current.type == EventType.DragPerform)
                            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                            {
                                var newPath = DragAndDrop.paths[0];
                                ViewPresenter.scriptsFolder = newPath;
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                            }

                        UnityEngine.Event.current.Use();
                    }
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("helpbox");
                {
                    GUILayout.Label("<color=#ffbe76>Prefab</color>", Styles.BigTitleStyle.Value);
                    GUILayout.BeginHorizontal();
                    {
                        ViewPresenter.generatePrefab = GUILayout.Toggle(
                            ViewPresenter.generatePrefab,
                            "Generate Prefab"
                        );
                    }
                    GUILayout.EndHorizontal();

                    if (ViewPresenter.generatePrefab)
                    {
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label("Prefab Folder", GUILayout.Width(150));
                            ViewPresenter.prefabFolder = GUILayout.TextArea(
                                ViewPresenter.prefabFolder
                            );
                        }
                        GUILayout.EndHorizontal();

                        EditorGUILayout.LabelField("Drag or write prefab folder");
                        var dragRect = EditorGUILayout.GetControlRect();
                        dragRect.height = 35;
                        GUI.Box(dragRect, string.Empty);
                        EditorGUILayout.LabelField(string.Empty);
                        if (
                            UnityEngine.Event.current.type == EventType.DragUpdated
                            && dragRect.Contains(UnityEngine.Event.current.mousePosition)
                        )
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                                if (DragAndDrop.paths[0] != "")
                                {
                                    var newPath = DragAndDrop.paths[0];
                                    ViewPresenter.prefabFolder = newPath;
                                    AssetDatabase.SaveAssets();
                                    AssetDatabase.Refresh();
                                    EditorSceneManager.MarkSceneDirty(
                                        SceneManager.GetActiveScene()
                                    );
                                }
                        }
                    }
                }
                GUILayout.EndVertical();

                if (!ViewPresenter.GetComponent<ExtraBinds>())
                    if (GUILayout.Button("Add Custom Binds", GUILayout.Height(30)))
                    {
                        ViewPresenter.gameObject.AddComponent<ExtraBinds>();
                        EditorUtility.SetDirty(ViewPresenter.gameObject);
                        EditorSceneManager.MarkSceneDirty(ViewPresenter.gameObject.scene);
                    }

                var fileFullPath =
                    ViewPresenter.scriptsFolder + "/" + ViewPresenter.scriptName + ".cs";
                if (File.Exists(fileFullPath))
                {
                    var scriptObject = AssetDatabase.LoadAssetAtPath<MonoScript>(fileFullPath);
                    if (GUILayout.Button("Open Script", GUILayout.Height(30)))
                        AssetDatabase.OpenAsset(scriptObject);
                    if (GUILayout.Button("Ping Script", GUILayout.Height(30)))
                        EditorGUIUtility.PingObject(scriptObject);
                    if (GUILayout.Button("Select Script", GUILayout.Height(30)))
                        Selection.activeObject = scriptObject;
                }
                else
                {
                    if (ViewPresenter.GetType() != typeof(ViewPresenter))
                    {
                        var scriptPath = AssetDatabase
                            .FindAssets($"t:{nameof(MonoScript)}")
                            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                            .Where(path =>
                                path.Contains(ViewPresenter.GetType().Name)
                                && !path.EndsWith("Designer.cs")
                            )
                            .FirstOrDefault(path =>
                                AssetDatabase.LoadAssetAtPath<MonoScript>(path).GetClass()
                                == ViewPresenter.GetType()
                            );
                        if (string.IsNullOrEmpty(scriptPath))
                            ViewPresenter.scriptsFolder = Path.GetDirectoryName(scriptPath);
                    }
                }

                if (GUILayout.Button("Generate Code", GUILayout.Height(30)))
                {
                    GenViewPresenterCodeUtility.Generate(ViewPresenter);
                    GUIUtility.ExitGUI();
                }
            }
            GUILayout.EndVertical();
        }

        private static Type[] FindAllIAppTypes()
        {
            var appType = typeof(IApp);
            return AppDomain
                .CurrentDomain.GetAssemblies()
                .Where(x => !x.FullName.Contains("UnityEngine"))
                .SelectMany(x => x.GetTypes())
                .Where(x => !x.IsAbstract && appType.IsAssignableFrom(x))
                .ToArray();
        }

        private static Type[] FindAllViewPresenterTypes()
        {
            var type = typeof(ViewPresenter);
            return AppDomain
                .CurrentDomain.GetAssemblies()
                .Where(x => !x.FullName.Contains("UnityEngine"))
                .SelectMany(x => x.GetTypes())
                .Where(x =>
                    x.GetAttribute<ViewPresenterChildAttribute>() != null
                    && type.IsAssignableFrom(x)
                )
                .ToArray();
        }
    }
}
