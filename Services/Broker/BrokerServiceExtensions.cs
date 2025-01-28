using Microsoft.Extensions.DependencyInjection;

namespace UpdateClientService.API.Services.Broker
{
    public static class BrokerServiceExtensions
    {
        public static IServiceCollection AddBrokerService(this IServiceCollection services)
        {
            return ServiceCollectionServiceExtensions.AddScoped<IReportFailedPingsJob, ReportFailedPingsJob>(ServiceCollectionServiceExtensions.AddScoped<IPingStatisticsService, PingStatisticsService>(ServiceCollectionServiceExtensions.AddScoped<IBrokerService, BrokerService>(services)));
        }
    }
}
