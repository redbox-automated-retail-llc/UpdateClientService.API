using Redbox.NetCore.Middleware.Http;

namespace UpdateClientService.API.Services.Broker
{
    public class PingStatisticsResponse : ApiBaseResponse
    {
        public PingStatistics PingStatistics { get; set; }
    }
}
