namespace Lazy
{
    public static class SerializerRegistry<T>
    {
        public static Serializer<T> Serializer { get; private set; }

        public static void Register(Serializer<T> serializer)
        {
            Serializer = serializer;
        }
    }
}
