using Lazy.Singleton;
using UnityEngine;

namespace Lazy.Res.Manager
{
    public class AssetBundleManager : Singleton<AssetBundleManager>
    {

        private AssetBundleManifest _manifest;

        private AssetBundleManager() {}

        public Hash128 GetAssetBundleHash(string assetBundleName)
        {
            return _manifest == null ? default : _manifest.GetAssetBundleHash(assetBundleName);
        }
    }
}