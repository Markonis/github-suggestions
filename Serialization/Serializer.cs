using Newtonsoft.Json;

namespace Serialization
{
    public static class Serializer
    {
        public static T DeepCopy<T>(T original)
        {
            return JsonConvert.DeserializeObject<T>(
                JsonConvert.SerializeObject(original));
        }
    }
}
