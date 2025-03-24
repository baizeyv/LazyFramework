using System;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.Build
{
    public class LazyBuildToolWindow : EditorWindow
    {
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
                window.minSize = new Vector2(Mathf.Max(stringLen * 11f - 250f, 500f), 777f);
            }
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
                            $"[Address Example: <color=#48dbfb>http://127.0.0.1:7373/</color> ]",
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
                    // TODO:
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
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
}
