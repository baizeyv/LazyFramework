using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Lazy.Editor.AssetEditor
{
    public static class AssetBundleSetName
    {
        [MenuItem("Assets/Lazy/AssetBundle/设置该文件夹及子文件夹abName为该文件夹名称")]
        private static void SetDirectoryBundleNameWithChild()
        {
            SetDirectoryBundleName();
        }

        [MenuItem("Assets/Lazy/AssetBundle/设置该文件夹及子文件夹abName为其所在文件夹名称")]
        private static void SetDirectoryBundleNameWithoutChild()
        {
            SetDirectoryBundleName(false);
        }

        [MenuItem("Assets/Lazy/AssetBundle/CleanSelectionAssetBundleName")]
        private static void ClearAssetBundleName()
        {
            var guids = Selection.assetGUIDs;
            foreach (var item in guids)
            {
                // 将 GUID 转换为 路径
                var assetPath = AssetDatabase.GUIDToAssetPath(item);
                var absolutePath = Path.Combine(
                    Application.dataPath,
                    assetPath.Substring("Assets/".Length)
                );

                if (File.Exists(absolutePath))
                {
                    var ai = AssetImporter.GetAtPath(assetPath);
                    if (!string.IsNullOrEmpty(ai.assetBundleName))
                    {
                        ai.assetBundleName = null;
                        EditorUtility.SetDirty(ai);
                    }

                    Debug.Log($"Clear File: {assetPath}");
                }
                else if (Directory.Exists(absolutePath))
                {
                    var aiDir = AssetImporter.GetAtPath(assetPath);
                    if (!string.IsNullOrEmpty(aiDir.assetBundleName))
                    {
                        aiDir.assetBundleName = null;
                        EditorUtility.SetDirty(aiDir);
                    }

                    Debug.Log($"Clear Folder: {assetPath}");

                    // 获取所有文件夹
                    var folderPaths = Directory.GetDirectories(
                        absolutePath,
                        "*",
                        SearchOption.AllDirectories
                    );
                    foreach (var folderPath in folderPaths)
                    {
                        var ai = AssetImporter.GetAtPath(
                            AssetBundlePathHelper.GetAssetPath(folderPath)
                        );
                        if (!string.IsNullOrEmpty(ai.assetBundleName))
                        {
                            ai.assetBundleName = null;
                            EditorUtility.SetDirty(ai);
                        }
                    }

                    // 获取所有文件
                    var assetPaths = Directory
                        .GetFiles(absolutePath, "*", SearchOption.AllDirectories)
                        .Where(path =>
                            !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
                            && !path.EndsWith(".DS_Store", StringComparison.OrdinalIgnoreCase)
                        )
                        .ToArray();
                    foreach (var ast in assetPaths)
                    {
                        var ai = AssetImporter.GetAtPath(AssetBundlePathHelper.GetAssetPath(ast));
                        if (!string.IsNullOrEmpty(ai.assetBundleName))
                        {
                            ai.assetBundleName = null;
                            EditorUtility.SetDirty(ai);
                        }

                        Debug.Log($"File: {ast}");
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("已清空所有选中的资产AB名");
        }

        [MenuItem("Assets/Lazy/AssetBundle/SetSelectionSameAssetBundleName")]
        private static void SetSelectionSameBundleName()
        {
            // 获取所有选中 文件、文件夹的 GUID
            var guids = Selection.assetGUIDs;
            var firstName = "";
            foreach (var guid in guids)
            {
                // 将 GUID 转换为 路径
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var absolutePath = Path.Combine(
                    Application.dataPath,
                    assetPath.Substring("Assets/".Length)
                );

                if (File.Exists(absolutePath))
                {
                    var ai = AssetImporter.GetAtPath(assetPath);
                    // 使用 Path.ChangeExtension 去掉扩展名
                    var bundleName = Path.ChangeExtension(assetPath, null)
                        .Replace(PathSetting.AssetBundlesPath, "");
                    if (!ai.assetBundleName.Equals(bundleName))
                    {
                        if (string.IsNullOrEmpty(firstName))
                        {
                            ai.assetBundleName = bundleName;
                            firstName = bundleName;
                        }
                        else
                        {
                            ai.assetBundleName = firstName;
                        }

                        EditorUtility.SetDirty(ai);
                    }

                    Debug.Log($"File: {assetPath}");
                }
                else if (Directory.Exists(absolutePath))
                {
                    // 获取所有文件
                    var assetPaths = Directory
                        .GetFiles(absolutePath, "*", SearchOption.AllDirectories)
                        .Where(path =>
                            !path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
                            && !path.EndsWith(".DS_Store", StringComparison.OrdinalIgnoreCase)
                        )
                        .ToArray();
                    foreach (var ast in assetPaths)
                    {
                        var getAssetPath = AssetBundlePathHelper.GetAssetPath(ast);
                        var ai = AssetImporter.GetAtPath(getAssetPath);
                        // 使用 Path.ChangeExtension 去掉扩展名
                        var bundleName = Path.ChangeExtension(getAssetPath, null)
                            .Replace(PathSetting.AssetBundlesPath, "");
                        if (!ai.assetBundleName.Equals(bundleName))
                        {
                            if (string.IsNullOrEmpty(firstName))
                            {
                                ai.assetBundleName = bundleName;
                                firstName = bundleName;
                            }
                            else
                            {
                                ai.assetBundleName = firstName;
                            }

                            EditorUtility.SetDirty(ai);
                        }

                        Debug.Log($"File: {ast}");
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("设置所有AB名为：" + firstName);
        }

        private static void SetDirectoryBundleName(bool childSame = true)
        {
            // # 获取所有选中的文件、文件夹的GUID
            var guids = Selection.assetGUIDs;
            // # 获取第一个文件夹的GUID
            var allFolderGuid = guids.Where(item =>
            {
                // # 将 GUID 转换为 路径
                var assetPath = AssetDatabase.GUIDToAssetPath(item);
                var absolutePath = Path.Combine(
                    Application.dataPath,
                    assetPath.Substring("Assets/".Length)
                );
                return Directory.Exists(absolutePath);
            });

            foreach (var folderGuid in allFolderGuid)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(folderGuid);
                HandleOneTime(assetPath, "", childSame);
            }
        }

        /// <summary>
        /// * 处理一次
        /// </summary>
        private static void HandleOneTime(
            string assetPath,
            string bundleName = null,
            bool childSame = true
        )
        {
            var relativePath = assetPath.Substring("Assets/".Length);
            var absolutePath = Path.Combine(Application.dataPath, relativePath);
            var files = Directory.GetFiles(absolutePath);
            var dirs = Directory.GetDirectories(absolutePath);
            if (string.IsNullOrEmpty(bundleName))
                bundleName = relativePath
                    .Substring(PathSetting.AssetBundlesPath.Substring("Assets/".Length).Length)
                    .ToLower();

            foreach (var dir in dirs)
            {
                var str = "Assets/" + dir.Substring(Application.dataPath.Length + 1);
                if (!childSame)
                    HandleOneTime(str, null, false);
                else
                    HandleOneTime(str, bundleName);
            }

            foreach (var file in files)
            {
                if (file.EndsWith(".meta"))
                    continue;
                var rPath = file.Substring(Application.dataPath.Length);
                var ai = AssetImporter.GetAtPath($"Assets{rPath}".Replace(@"\", "/"));
                if (!ai.assetBundleName.Equals(bundleName))
                {
                    ai.assetBundleName = bundleName;
                    EditorUtility.SetDirty(ai);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"设置{assetPath}下所有AssetBundleName为{bundleName}");
        }
    }
}
