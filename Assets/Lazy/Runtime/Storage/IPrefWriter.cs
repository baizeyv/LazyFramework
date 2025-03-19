namespace Lazy.Serializer
{
    public interface IPrefWriter
    {
        void WriteString(string key, string value);

        void WriteInt(string key, int value);

        void WriteFloat(string key, float value);

        void Save();
    }
}
