using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT
{
    public interface IIoTStatisticsService
    {
        Task<bool> RecordConnectionAttempt();

        Task<bool> RecordConnectionSuccess();

        Task<bool> RecordDisconnection();

        Task<bool> RecordConnectionException(string exceptionMessage);

        Task<IoTStatisticsSummaryResponse> GetIoTStatisticsSummaryResponse();
    }
}
