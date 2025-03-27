using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Lazy.Editor.AssetEditor;
using Lazy.Utility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditorInternal;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Lazy.Editor.Build
{
    public class LazyBuildToolWindow : EditorWindow
    {
        private Vector2 _scrollPosition = Vector2.zero;

        private ReorderableList _reorderableBundleList;

        private List<string> _localBundles;

        [MenuItem("Lazy/Build Editor _F2", false)]
        private static void OpenBuildTool()
        {
            if (HasOpenInstances<LazyBuildToolWindow>())
            {
                GetWindow<LazyBuildToolWindow>("Build Editor").Close();
            }
            else
            {
                var window = GetWindow<LazyBuildToolWindow>("Build Editor");
                var stringLen = StringLen(LazyBuildTool.BuildPath);
                window.minSize = new Vector2(Mathf.Max(stringLen * 11f - 250f, 500f), 150);
            }
        }

        private void OnEnable()
        {
            _reorderableBundleList = new ReorderableList(
                _localBundles,
                typeof(string),
                false,
                true,
                false,
                false
            )
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, "Local Asset-Bundles List");
                },
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    EditorGUI.LabelField(rect, _localBundles[index]);
                },
            };
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical("helpbox");
            {
                GUILayout.BeginHorizontal("box");
                {
                    GUILayout.Label(
                        "<color=#7bed9f>Lazy Build Tool</color>",
                        Styles.BigTitleStyle.Value
                    );
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(
                        $"[ <color=#1e90ff>{Application.productName}</color> ]",
                        Styles.BigTitleStyle.Value
                    );
                }
                GUILayout.EndHorizontal();
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                {
                    // ###########################################
                    GUILayout.BeginVertical("helpbox");
                    {
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label(
                                "<color=#48dbfb>Set Platform</color>",
                                Styles.BigTitleStyle.Value
                            );
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(
                                $"[Current: <color=#48dbfb>{EditorUserBuildSettings.activeBuildTarget}</color> ]",
                                Styles.BigTitleStyle.Value
                            );
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label("Use Current Platform");
                            GUILayout.FlexibleSpace();
                            LazyBuildTool.ExportCurrentPlatform = EditorGUILayout.Toggle(
                                " ",
                                EditorPrefs.GetBool(EditorConstant.ExportCurrentPlatformKey, true)
                            );
                        }
                        GUILayout.EndHorizontal();
                        if (
                            EditorPrefs.GetBool(EditorConstant.ExportCurrentPlatformKey, true)
                            != LazyBuildTool.ExportCurrentPlatform
                        )
                            EditorPrefs.SetBool(
                                EditorConstant.ExportCurrentPlatformKey,
                                LazyBuildTool.ExportCurrentPlatform
                            );
                        if (LazyBuildTool.ExportCurrentPlatform)
                            LazyBuildTool.BuildTarget = EditorUserBuildSettings.activeBuildTarget;

                        if (!LazyBuildTool.ExportCurrentPlatform)
                        {
                            // # 不使用当前平台导出
                            var enumValues = Enum.GetValues(typeof(BuildTarget));
                            LazyBuildTool.Index = Array.FindIndex(
                                (BuildTarget[])enumValues,
                                x =>
                                    x.ToString()
                                    == EditorPrefs.GetString(EditorConstant.ExportPlatformKey, "")
                            );
                            if (LazyBuildTool.Index < 0)
                                LazyBuildTool.Index = Array.FindIndex(
                                    (BuildTarget[])enumValues,
                                    x => x.ToString() == LazyBuildTool.BuildTarget.ToString()
                                );
                            LazyBuildTool.Index = EditorGUILayout.Popup(
                                LazyBuildTool.Index,
                                LazyBuildTool.OptionNames
                            );

                            if (
                                LazyBuildTool.Options[LazyBuildTool.Index].ToString()
                                != EditorPrefs.GetString(EditorConstant.ExportPlatformKey, "")
                            )
                            {
                                EditorPrefs.SetString(
                                    EditorConstant.ExportPlatformKey,
                                    LazyBuildTool.Options[LazyBuildTool.Index].ToString()
                                );
                                EditorUserBuildSettings.SwitchActiveBuildTarget(
                                    GetBuildTargetGroup(LazyBuildTool.Options[LazyBuildTool.Index]),
                                    LazyBuildTool.Options[LazyBuildTool.Index]
                                );
                            }

                            LazyBuildTool.BuildTarget = LazyBuildTool.Options[LazyBuildTool.Index];
                        }

                        if (LazyBuildTool.BuildTarget == BuildTarget.Android)
                        {
                            GUILayout.BeginHorizontal("box");
                            {
                                GUILayout.Label("Export Project");
                                GUILayout.FlexibleSpace();
                                LazyBuildTool.ExportAndroidProject = GUILayout.Toggle(
                                    LazyBuildTool.ExportAndroidProject,
                                    ""
                                );
                            }
                            GUILayout.EndHorizontal();
                            if (LazyBuildTool.ExportAndroidProject)
                            {
                                GUILayout.BeginHorizontal("box");
                                {
                                    GUILayout.Label(
                                        $"Export Path: {LazyBuildTool.ExportAndroidPath}"
                                    );
                                    GUILayout.FlexibleSpace();
                                    if (GUILayout.Button("..."))
                                    {
                                        var path = EditorUtility.OpenFolderPanel(
                                            "Set Export Android Path",
                                            LazyBuildTool.ExportAndroidPath,
                                            LazyBuildTool.ExportAndroidPath
                                        );
                                        if (string.IsNullOrEmpty(path))
                                            path = $"{Application.dataPath}/../../ex";
                                        LazyBuildTool.ExportAndroidPath = path;
                                    }
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                    GUILayout.EndVertical();
                    // ###########################################
                    GUILayout.BeginVertical("helpbox");
                    {
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label(
                                "<color=#48dbfb>Set Output Path</color>",
                                Styles.BigTitleStyle.Value
                            );
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(
                                $"[Example: <color=#48dbfb>{Application.dataPath}</color> ]",
                                Styles.BigTitleStyle.Value
                            );
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label($"{LazyBuildTool.BuildPath}");
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("..."))
                            {
                                var path = EditorUtility.OpenFolderPanel(
                                    "Set Build Root Path",
                                    LazyBuildTool.BuildPath,
                                    LazyBuildTool.BuildPath
                                );
                                LazyBuildTool.BuildPath = path;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    // ###########################################
                    GUILayout.BeginVertical("helpbox");
                    {
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label(
                                "<color=#48dbfb>Set Version</color>",
                                Styles.BigTitleStyle.Value
                            );
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(
                                $"[Example: <color=#48dbfb>1.0.0</color> ]",
                                Styles.BigTitleStyle.Value
                            );
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label("Build Version: ");
                            LazyBuildTool.ToVersion = EditorGUILayout.TextField(
                                LazyBuildTool.ToVersion
                            );
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label("Build Code:    ");
                            LazyBuildTool.CodeVersion = EditorGUILayout.TextField(
                                LazyBuildTool.CodeVersion
                            );
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndVertical();
                    // ###########################################
                    GUILayout.BeginVertical("helpbox");
                    {
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label(
                                "<color=#48dbfb>Hot Update</color>",
                                Styles.BigTitleStyle.Value
                            );
                            GUILayout.FlexibleSpace();
                            GUILayout.Label(
                                $"[Address Example: <color=#48dbfb>http://127.0.0.1:7373/Remote</color> ]",
                                Styles.BigTitleStyle.Value
                            );
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label("Remote Address: ");
                            LazyBuildTool.AssetRemoteAddress = EditorGUILayout.TextField(
                                LazyBuildTool.AssetRemoteAddress
                            );
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label("Enable Hot Update");
                            GUILayout.FlexibleSpace();
                            LazyBuildTool.EnableHotUpdate = EditorGUILayout.Toggle(
                                " ",
                                LazyBuildTool.EnableHotUpdate
                            );
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal("box");
                        {
                            GUILayout.Label("Enable Package");
                            GUILayout.FlexibleSpace();
                            LazyBuildTool.EnablePackage = EditorGUILayout.Toggle(
                                " ",
                                LazyBuildTool.EnablePackage
                            );
                        }
                        GUILayout.EndHorizontal();
                        if (LazyBuildTool.EnablePackage)
                        {
                            GUILayout.BeginHorizontal("box");
                            {
                                GUILayout.Label("Select Package, Splitter: _");
                                GUILayout.FlexibleSpace();
                                LazyBuildTool.EnableOptionalPackage = EditorGUILayout.Toggle(
                                    " ",
                                    LazyBuildTool.EnableOptionalPackage
                                );
                            }
                            GUILayout.EndHorizontal();
                            if (LazyBuildTool.EnableOptionalPackage)
                            {
                                GUILayout.BeginHorizontal("box");
                                {
                                    GUILayout.Label("0_1 => Package_0.zip Package_1.zip");
                                    GUILayout.FlexibleSpace();
                                    LazyBuildTool.OptionalPackage = EditorGUILayout.TextField(
                                        LazyBuildTool.OptionalPackage
                                    );
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                    GUILayout.EndVertical();

                    // ####################################################
                    if (LazyBuildTool.EnableHotUpdate)
                    {
                        GUILayout.BeginVertical("helpbox");
                        {
                            GUILayout.BeginHorizontal("box");
                            {
                                GUILayout.Label("Save To Local: ");
                                GUILayout.FlexibleSpace();
                                var buttonRect = GUILayoutUtility.GetRect(200, 20);
                                if (GUI.Button(buttonRect, "Select Bundles"))
                                {
                                    LazyBuildTool.GetAllAssetBundle();
                                    LazyBuildTool.AbSelectedOptions = new List<bool>(
                                        LazyBuildTool.AbOptions.Count
                                    );
                                    var localStr = EditorPrefs.GetString(
                                        EditorConstant.LocalAssetBundlesKey,
                                        ""
                                    );
                                    if (string.IsNullOrEmpty(localStr))
                                    {
                                        // # 没有要存在本地的
                                        for (var i = 0; i < LazyBuildTool.AbOptions.Count; i++)
                                            LazyBuildTool.AbSelectedOptions.Add(false);
                                    }
                                    else
                                    {
                                        // # 有要存在本地的
                                        var array = localStr.Split(';');
                                        foreach (var bundleName in LazyBuildTool.AbOptions)
                                            LazyBuildTool.AbSelectedOptions.Add(
                                                array.Contains(bundleName)
                                            );
                                    }

                                    PopupWindow.Show(
                                        buttonRect,
                                        new MultiSelectPopup(OnBundleSelectClose)
                                    );
                                }
                            }
                            GUILayout.EndHorizontal();
                            _reorderableBundleList.DoLayoutList();
                        }
                        GUILayout.EndVertical();
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.BeginHorizontal("box");
                {
                    if (GUILayout.Button("Open Output Folder"))
                        Process.Start(Path.GetFullPath(LazyBuildTool.BuildPath));
                    if (GUILayout.Button("Open SandBox Folder"))
                        Process.Start(Application.persistentDataPath);
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal("box");
                {
                    if (GUILayout.Button("Build"))
                    {
                        LazyBuildTool.WriteAppVersion();
                        EditorApplication.delayCall += () =>
                        {
                            AssetDatabase.Refresh();
                            AssetBundleBuildTool.BuildAllAssetBundles();
                            LazyBuildTool.Build();
                            LazyBuildTool.WriteAssetVersion();
                        };
                    }

                    if (GUILayout.Button("Build HotUpdate"))
                    {
                        if (string.IsNullOrEmpty(LazyBuildTool.BuildPath))
                        {
                            EditorUtility.DisplayDialog(
                                "Build HotUpdate",
                                "Build Path Cannot Be Null",
                                "OK"
                            );
                        }
                        else
                        {
                            if (
                                EditorUtility.DisplayDialog(
                                    "Build HotUpdate",
                                    "Are you sure ? Version: " + LazyBuildTool.ToVersion,
                                    "OK"
                                )
                            )
                                EditorApplication.delayCall += () =>
                                {
                                    AssetDatabase.Refresh();
                                    AssetBundleBuildTool.BuildAllAssetBundles();
                                    LazyBuildTool.BuildHotUpdate();
                                };
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private void OnBundleSelectClose()
        {
            _localBundles.Clear();
            for (var i = 0; i < LazyBuildTool.AbOptions.Count; i++)
                if (LazyBuildTool.AbSelectedOptions[i])
                    _localBundles.Add(LazyBuildTool.AbOptions[i]);

            var val = string.Join(";", _localBundles);
            EditorPrefs.SetString(EditorConstant.LocalAssetBundlesKey, val);

            Repaint();
        }

        private static BuildTargetGroup GetBuildTargetGroup(BuildTarget target)
        {
            var targetGroup = BuildPipeline.GetBuildTargetGroup(target);

            if (targetGroup != BuildTargetGroup.Unknown)
            {
                return targetGroup;
            }
            else
            {
                Log.Log.MsgE($"Could not find BuildTargetGroup for BuildTarget {target}");
                return default;
            }
        }

        public static int StringLen(string str)
        {
            var realLength = 0;
            foreach (var c in str)
                if (c >= 0 && c <= 128)
                    realLength += 1;
                else
                    realLength += 2;

            return realLength;
        }
    }

    public class MultiSelectPopup : PopupWindowContent
    {
        private Vector2 _scrollPos = Vector2.zero;

        private Action _onCloseCallback;

        public MultiSelectPopup(Action onClose)
        {
            _onCloseCallback = onClose;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(200, Math.Min(LazyBuildTool.AbOptions.Count * 25, 200));
        }

        public override void OnGUI(Rect rect)
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginVertical("helpbox");
                {
                    for (var i = 0; i < LazyBuildTool.AbOptions.Count; i++)
                        LazyBuildTool.AbSelectedOptions[i] = EditorGUILayout.Toggle(
                            LazyBuildTool.AbOptions[i],
                            LazyBuildTool.AbSelectedOptions[i]
                        );
                }
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndScrollView();
        }

        public override void OnClose()
        {
            _onCloseCallback.Fire();
        }
    }
}
