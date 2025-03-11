using System.Collections.Generic;

namespace Lazy.Res.HotUpdate
{
    public class AppVersion
    {
        /// <summary>
        /// * 版本号
        /// </summary>
        public string Version;

        /// <summary>
        /// * 远程资产地址
        /// </summary>
        public string AssetRemoteAddress;

        /// <summary>
        /// * 是否启用热更新
        /// </summary>
        public bool EnableHotUpdate;

        public List<string> HotUpdateVersions;

        /// <summary>
        /// * 是否启用分包
        /// </summary>
        public bool EnablePackage;

        /// <summary>
        /// * 子包列表
        /// </summary>
        public List<string> SubPackages;

        public AppVersion() { }

        public AppVersion(
            string version,
            string assetRemoteAddress = null,
            bool enableHotUpdate = false,
            List<string> hotUpdateVersions = null,
            bool enablePackage = false,
            List<string> subPackages = null
        )
        {
            Version = version;
            AssetRemoteAddress = assetRemoteAddress;
            EnableHotUpdate = enableHotUpdate;
            HotUpdateVersions = hotUpdateVersions;
            EnablePackage = enablePackage;
            SubPackages = subPackages;
        }
    }
}
