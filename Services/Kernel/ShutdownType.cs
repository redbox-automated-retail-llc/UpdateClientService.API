using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UpdateClientService.API.Services.Kernel
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ShutdownType
    {
        Reboot,
        Shutdown,
    }
}
