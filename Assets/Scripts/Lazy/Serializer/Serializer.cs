namespace Lazy.Serializer
{
    public abstract class Serializer<T>
    {
        public virtual string Serialize(T t)
        {
            return string.Empty;
        }

        public virtual T Deserialize(string data)
        {
            return default!;
        }
    }
}