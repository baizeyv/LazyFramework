namespace Lazy.Pool.Factory
{
    public interface IObjectFactory<T>
    {
        T Create();
    }
}