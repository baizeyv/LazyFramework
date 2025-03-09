using Lazy.Pool;
using Lazy.Res.Loader;

namespace Lazy.Res
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
    }
}
