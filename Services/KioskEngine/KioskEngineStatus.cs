using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UpdateClientService.API.Services.KioskEngine
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum KioskEngineStatus
    {
        Unknown,
        Running,
        Stopped,
    }
}
