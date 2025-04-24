using System;
using System.Collections.Generic;
using Lazy;

namespace Lazy
{
    public static class AppConfig
    {
        /// <summary>
        /// * 本地版本
        /// </summary>
        public static AppVersion LocalVersion = new();

        /// <summary>
        /// * 远程版本
        /// </summary>
        public static AppVersion RemoteVersion = new();

        /// <summary>
        /// * 远程AssetBundle映射字典
        /// </summary>
        public static Dictionary<string, AssetMapping> RemoteAssetBundleMapping = new();

        /// <summary>
        /// * 判断版本号大小, 1->v1大, -1->v2da, 0->same
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static int CompareVersion(string v1, string v2)
        {
            var v1Components = v1.Split('.');
            var v2Components = v2.Split('.');
            var maxLength = Math.Max(v1Components.Length, v2Components.Length);

            for (var i = 0; i < maxLength; i++)
            {
                var vA = i < v1Components.Length ? Convert.ToInt32(v1Components[i]) : 0;
                var vB = i < v2Components.Length ? Convert.ToInt32(v2Components[i]) : 0;

                if (vA < vB)
                    return -1;
                if (vA > vB)
                    return 1;
            }

            return 0;
        }
    }
}
