using Redbox.NetCore.Middleware.Http;

namespace UpdateClientService.API.Services.Configuration
{
    public class ConfigurationStatusResponse : ApiBaseResponse
    {
        public ConfigurationStatus ConfigurationStatus { get; set; }
    }
}
