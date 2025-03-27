using Newtonsoft.Json;

namespace Lazy
{
    public class Constant
    {
        public static JsonSerializerSettings JsonSetting =
            new() { DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate };
    }
}
