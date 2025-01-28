using Microsoft.Extensions.DependencyInjection;

namespace UpdateClientService.API.Services.IoT
{
    public static class MqttExtensions
    {
        public static IServiceCollection AddMqttService(this IServiceCollection serviceCollection)
        {
            ServiceCollectionServiceExtensions.AddSingleton<IMqttProxy, MqttProxy>(serviceCollection);
            ServiceCollectionServiceExtensions.AddSingleton<IIoTStatisticsService, IoTStatisticsService>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<MqttProxyJob>(serviceCollection);
            return serviceCollection;
        }
    }
}
