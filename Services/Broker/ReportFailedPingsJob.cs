using Coravel.Invocable;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.Broker
{
    public class ReportFailedPingsJob : IReportFailedPingsJob, IInvocable
    {
        private readonly IPingStatisticsService _pingStatisticsService;

        public ReportFailedPingsJob(IPingStatisticsService pingStatisticsService)
        {
            this._pingStatisticsService = pingStatisticsService;
        }

        public async Task Invoke()
        {
            int num = await this._pingStatisticsService.ReportFailedPing() ? 1 : 0;
        }
    }
}
