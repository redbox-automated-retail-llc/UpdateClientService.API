using System.Threading.Tasks;

namespace UpdateClientService.API.Services.Broker
{
    public interface IPingStatisticsService
    {
        Task<bool> RecordPingStatistic(bool pingSuccess);

        Task<PingStatisticsResponse> GetPingStatisticsResponse();

        Task<LastSuccessfulPingResponse> GetLastSuccessfulPing();

        Task<bool> ReportFailedPing();
    }
}
