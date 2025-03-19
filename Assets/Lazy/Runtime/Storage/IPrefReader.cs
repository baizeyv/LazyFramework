namespace Lazy.Serializer
{
    public interface IPrefReader
    {
        string ReadString(string key, string defaultValue = "");

        int ReadInt(string key, int defaultValue = 0);

        float ReadFloat(string key, float defaultValue = 0f);

        bool HasKey(string key);
    }
}
