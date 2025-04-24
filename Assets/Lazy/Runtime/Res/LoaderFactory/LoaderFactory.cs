using Lazy;

namespace Lazy
{
    public static class LoaderFactory
    {
        public static ResourcesLoader CreateLoader(string resourcePath)
        {
            var loader = SafeObjectPool<ResourcesLoader>.Instance.Obtain();
            loader.Setup(resourcePath);
            return loader;
        }

        public static void ReleaseLoader(ResourcesLoader loader)
        {
            SafeObjectPool<ResourcesLoader>.Instance.Free(loader);
        }

        public static AssetBundleLoader CreateABLoader(string assetBundlePath)
        {
            var loader = new AssetBundleLoader();
            loader.Setup(assetBundlePath);
            return loader;
        }
    }
}
