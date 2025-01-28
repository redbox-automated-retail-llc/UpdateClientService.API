using Newtonsoft.Json;

namespace UpdateClientService.API
{
    public static class JsonExtensions
    {
        public static string ToJsonIndented(this object obj)
        {
            return JsonConvert.SerializeObject(obj, (Formatting)1, new JsonSerializerSettings()
            {
                NullValueHandling = (NullValueHandling)1
            });
        }

        public static string ToJson(this object obj)
        {
            return JsonConvert.SerializeObject(obj, (Formatting)0, new JsonSerializerSettings()
            {
                NullValueHandling = (NullValueHandling)1
            });
        }

        public static T ToObject<T>(this string obj) => JsonConvert.DeserializeObject<T>(obj);
    }
}
