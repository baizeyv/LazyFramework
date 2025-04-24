using Lazy;
using Newtonsoft.Json;

namespace Lazy
{
    /// <summary>
    /// * Json序列化器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonSerializer<T> : Serializer<T>
    {
        private static readonly JsonSerializerSettings Settings =
            new() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate };

        public override string Serialize(T t)
        {
            return JsonConvert.SerializeObject(t, Settings);
        }

        public override T Deserialize(string data)
        {
            return JsonConvert.DeserializeObject<T>(data, Settings);
        }
    }
}
