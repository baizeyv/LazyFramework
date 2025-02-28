namespace Lazy.Serializer.CustomSerializers
{
    public class StringSerializer : Serializer<string>
    {
        public override string Serialize(string t)
        {
            return t;
        }

        public override string Deserialize(string data)
        {
            return data;
        }
    }
}