namespace Lazy.Pool.GameObject
{
    /// <summary>
    /// * 放回池中的请求
    /// </summary>
    public struct DespawnRequest
    {
        /// <summary>
        /// * 克隆体 Wrapper
        /// </summary>
        internal GameObjectPoolable Poolable;

        /// <summary>
        /// * 延迟回收的时间 (延迟多少)
        /// </summary>
        internal float TimeToDespawn;
    }
}
