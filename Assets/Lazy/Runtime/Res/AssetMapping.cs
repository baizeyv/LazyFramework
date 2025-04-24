using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

namespace Lazy
{
    [Preserve] // * 防止该类被代码裁剪
    public class AssetMapping
    {
        /// <summary>
        /// * AssetBundle包名
        /// </summary>
        public string AssetBundleName;

        /// <summary>
        /// * 资源路径 (Assets/AssetBundles/foo/bar)
        /// </summary>
        public string[] AssetPath;

        /// <summary>
        /// * 版本名称 (For Example: 1.0.0)
        /// </summary>
        public string VersionName;

        /// <summary>
        /// * 资源大小 (byte)
        /// </summary>
        public long Size;

        /// <summary>
        /// * 资源 MD5 码
        /// </summary>
        public string MD5;

        /// <summary>
        /// * 分包使用的包
        /// </summary>
        public string Package;

        /// <summary>
        /// * 热更已经更新的标志
        /// </summary>
        public bool Updated;

        public AssetMapping(
            string assetBundleName,
            string[] assetPath,
            string versionName,
            long size,
            string md5,
            string package,
            bool updated
        )
        {
            AssetBundleName = assetBundleName;
            AssetPath = assetPath;
            VersionName = versionName;
            Size = size;
            MD5 = md5;
            Package = package;
            Updated = updated;
        }

        public static bool Compare(AssetMapping a, AssetMapping b)
        {
            return a.AssetBundleName.Equals(b.AssetBundleName)
                && a.VersionName.Equals(b.VersionName)
                && a.Size == b.Size
                && a.MD5.Equals(b.MD5)
                && a.Updated == b.Updated
                && a.Package.Equals(b.Package)
                && a.AssetPath.SequenceEqual(b.AssetPath);
        }
    }

    public static class AssetBundleMapping
    {
        public static Dictionary<string, AssetMapping> Mappings { get; set; } = new();

        public static bool Compare(
            Dictionary<string, AssetMapping> a,
            Dictionary<string, AssetMapping> b
        )
        {
            return a.Count == b.Count
                && a.All(x =>
                    b.TryGetValue(x.Key, out var value) && AssetMapping.Compare(value, x.Value)
                );
        }
    }

    public static class ResourceMapping
    {
        public static Dictionary<string, string[]> Mappings { get; set; } = new();
    }
}
