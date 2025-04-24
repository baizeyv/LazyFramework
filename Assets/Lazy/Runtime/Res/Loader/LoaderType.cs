namespace Lazy
{
    /// <summary>
    /// * 加载器加载类型
    /// </summary>
    public enum ResourceLoaderType
    {
        None,
        Sync, // # 同步加载
        Async // # 异步加载
        ,
    }

    public enum AssetBundleLoaderType
    {
        None,
        LocalSync, // # 本地同步加载
        LocalAsync, // # 本地异步加载
        RemoteAsync // # 远程异步加载
        ,
    }
}
