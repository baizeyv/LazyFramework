namespace Lazy.Pool
{
    public interface IPool<T>
    {
        T Obtain();

        void Free(T obj);
    }
}