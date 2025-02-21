namespace Lazy.Pool.Factory
{
    public class DefaultObjectFactory<T> : IObjectFactory<T>
        where T : new()
    {
        public T Create()
        {
            return new T();
        }
    }
}