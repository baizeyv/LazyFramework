namespace Lazy.Pool.GameObject
{
    public struct GetPoolableArgument
    {
        public readonly GameObjectPoolable Poolable;

        /// <summary>
        /// * 结果是否可以为空
        /// </summary>
        public readonly bool IsResultNullable;

        public GetPoolableArgument(GameObjectPoolable poolable, bool isResultNullable)
        {
            Poolable = poolable;
            IsResultNullable = isResultNullable;
        }
    }
}
