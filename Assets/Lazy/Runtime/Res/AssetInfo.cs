using System;

namespace Lazy.Res
{
    /// <summary>
    /// * 资产信息结构体
    /// </summary>
    public struct AssetInfo
    {
        /// <summary>
        /// * 资产类型
        /// </summary>
        public readonly AssetType AssetType;

        /// <summary>
        /// * 资产名称
        /// </summary>
        public readonly string AssetName;

        /// <summary>
        /// * 直接资产请求路径相对路径,Assets开头的
        /// </summary>
        public readonly string[] AssetPath;

        /// <summary>
        /// * 直接AssetBundle请求路径(仅用于AssetBundle),完全路径
        /// </summary>
        public readonly string AssetBundlePath;

        /// <summary>
        /// * AssetBundle包名
        /// </summary>
        public readonly string AssetBundleName;

        public AssetInfo(
            AssetType assetType = default,
            string assetName = null,
            string[] assetPath = null,
            string assetBundlePathWithoutBundleName = null,
            string assetBundleName = null
        )
        {
            AssetType = assetType;
            AssetName = assetName;
            AssetPath = assetPath;
            AssetBundlePath = assetBundlePathWithoutBundleName + assetBundleName;
            AssetBundleName = assetBundleName;
        }
    }

    /// <summary>
    /// * 资产类型
    /// </summary>
    public enum AssetType
    {
        None,
        Resource,
        AssetBundle,
    }

    /// <summary>
    /// * 资产访问模式
    /// </summary>
    [Flags] // # 允许位运算
    public enum AssetAccessMode
    {
        None = 0b1,
        Unknown = 0b10,
        Resource = 0b100,
        LocalAssetBundle = 0b1000,
        RemoteAssetBundle = 0b10000,
    }
}
