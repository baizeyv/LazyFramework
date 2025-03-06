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
