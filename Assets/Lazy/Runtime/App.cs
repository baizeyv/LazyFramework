using Lazy.Manage;
using Lazy.Res;

namespace Lazy
{
    public static class App
    {
        private static AssetManager _assetManager;

        /// <summary>
        /// * 资产管理器
        /// </summary>
        public static AssetManager Asset
        {
            get { return _assetManager ??= ManagerCenter.Create(() => AssetManager.Instance); }
            set => _assetManager ??= value;
        }
    }
}
