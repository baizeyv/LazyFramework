using Lazy.Editor.AssetEditor;
using UnityEditor;

namespace Lazy.Editor
{
    public static class Helper
    {
        [MenuItem("Lazy/打包AssetBundles目录资源")]
        public static void BuildAssetBundles()
        {
            AssetBundleBuildTool.BuildAllAssetBundles();
        }
    }
}
