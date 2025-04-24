namespace Lazy
{
    public interface IObjectFactory<T>
    {
        T Create();
    }
}
