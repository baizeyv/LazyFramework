using Lazy.Utility;

namespace Lazy.Serializer.CustomSerializers
{
    public class FloatSerializer : Serializer<float>
    {
        public override string Serialize(float t)
        {
            return t.ToString();
        }

        public override float Deserialize(string data)
        {
            return data.TryParseTo(0f);
        }
    }
}