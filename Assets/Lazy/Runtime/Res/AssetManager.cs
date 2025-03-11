using System.Collections.Generic;
using System.IO;
using Lazy.Manage;
using Lazy.Singleton;
using UnityEngine;

namespace Lazy.Res
{
    public delegate void AssetLoadedCallback<T>(T obj)
        where T : Object;

    public class AssetManager : Singleton<AssetManager>, IManager
    {
        /// <summary>
        /// * 强制更改资产加载模式为远程（微信小游戏使用）
        /// </summary>
        public static bool ForceRemoteAssetBundle = false;

        public const string DirectorySuffix = "@Directory";

        /// <summary>
        /// * 是否试用Editor模式
        /// </summary>
        private bool _isEditorMode;

        public bool IsEditorMode
        {
            get
            {
#if UNITY_EDITOR
                return _isEditorMode
                    || UnityEditor.EditorPrefs.GetBool(
                        Application.dataPath.GetHashCode() + "IsEditorMode",
                        false
                    );
#endif
                return false;
            }
            set { _isEditorMode = value; }
        }

        /// <summary>
        /// * 如果信息合法,则为真
        /// </summary>
        /// <param name="assetInfo"></param>
        /// <returns></returns>
        public bool IsLegal(ref AssetInfo assetInfo)
        {
#if UNITY_EDITOR
            if (IsEditorMode)
            {
                if (assetInfo.AssetType == AssetType.Resource)
                    if (
                        assetInfo.AssetPath != null
                        || SearchAsset(assetInfo.AssetName, AssetAccessMode.Resource) != null
                    )
                        return true;

                if (assetInfo.AssetType == AssetType.AssetBundle)
                    if (
                        (assetInfo.AssetPath != null && assetInfo.AssetBundlePath != null)
                        || SearchAsset(assetInfo.AssetName, AssetAccessMode.LocalAssetBundle)
                            != null
                    )
                        return true;

                return false;
            }

#endif
            if (assetInfo.AssetType == AssetType.None)
                return false;
            if (assetInfo.AssetType == AssetType.Resource && assetInfo.AssetPath != null)
                return false;

            if (
                assetInfo.AssetType == AssetType.AssetBundle
                && (assetInfo.AssetPath == null || string.IsNullOrEmpty(assetInfo.AssetBundlePath))
            )
                return false;
            return true;
        }

        // TODO:

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnDestroy() { }

#if UNITY_EDITOR
        private List<string> _searchDirs = new();
        private List<string> _resourcesDirs = new();
        private List<string> _assetBundlesDirs = new();
        private Dictionary<string, string> _findAssetPaths = new();
        private Dictionary<string, string> _resourcesFindAssetPaths = new();
        private Dictionary<string, string> _assetBundlesFindAssetPaths = new();

        private string SearchAsset(
            string assetName,
            AssetAccessMode accessMode = AssetAccessMode.Unknown
        )
        {
            if (accessMode == AssetAccessMode.Unknown)
            {
                if (_findAssetPaths.TryGetValue(assetName, out var value))
                    return value;
            }
            else if (accessMode == AssetAccessMode.Resource)
            {
                if (_resourcesFindAssetPaths.TryGetValue(assetName, out var value))
                    return value;
            }
            else if (accessMode == AssetAccessMode.LocalAssetBundle)
            {
                if (_assetBundlesFindAssetPaths.TryGetValue(assetName, out var value))
                    return value;
            }

            if (_searchDirs.Count <= 0)
            {
                // # 获取项目中的所有文件夹路径
                var allFolders = UnityEditor.AssetDatabase.GetAllAssetPaths();
                foreach (var folderPath in allFolders)
                    if (Directory.Exists(folderPath) && folderPath.Contains("/Resources"))
                    {
                        _searchDirs.Add(folderPath);
                        _resourcesDirs.Add(folderPath);
                    }

                _searchDirs.Add(Path.Combine(PathSetting.AssetBundlesPath));
                _assetBundlesDirs.Add(Path.Combine(PathSetting.AssetBundlesPath));
            }

            // # 查找指定资源
            string[] dirs = null;
            if (accessMode == AssetAccessMode.Unknown)
                dirs = _searchDirs.ToArray();
            else if (accessMode == AssetAccessMode.Resource)
                dirs = _resourcesDirs.ToArray();
            else if (accessMode == AssetAccessMode.LocalAssetBundle)
                dirs = _assetBundlesDirs.ToArray();

            var guids = UnityEditor.AssetDatabase.FindAssets(assetName, dirs);
            foreach (var guid in guids)
            {
                // # 将 GUID 转换为路径
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(assetPath).Equals(assetName))
                {
                    if (accessMode == AssetAccessMode.Unknown)
                        _findAssetPaths[assetName] = assetPath;
                    else if (accessMode == AssetAccessMode.Resource)
                        _resourcesFindAssetPaths[assetName] = assetPath;
                    else if (accessMode == AssetAccessMode.LocalAssetBundle)
                        _assetBundlesFindAssetPaths[assetName] = assetPath;

                    Log.Log.MsgD("GET:" + assetPath);
                    return assetPath;
                }
            }

            return null;
        }
#endif
    }
}
