using Microsoft.Extensions.Logging;

namespace UpdateClientService.API.Services
{
    public static class SharedLogger
    {
        public static ILoggerFactory Factory { get; set; }
    }
}
