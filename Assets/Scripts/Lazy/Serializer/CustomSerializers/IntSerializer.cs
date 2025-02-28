
using Lazy.Utility;

namespace Lazy.Serializer.CustomSerializers
{
    public class IntSerializer : Serializer<int>
    {
        public override string Serialize(int t)
        {
            return t.ToString();
        }

        public override int Deserialize(string data)
        {
            return data.TryParseTo(0);
        }
    }
}