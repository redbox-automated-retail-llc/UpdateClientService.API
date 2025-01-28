using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UpdateClientService.API.Services.Configuration
{
    public static class ConfigurationServiceExtensions
    {
        public static IServiceCollection AddConfigurationService(
          this IServiceCollection serviceCollection,
          IConfiguration configuration)
        {
            OptionsConfigurationServiceCollectionExtensions.Configure<KioskConfigurationSettings>(serviceCollection, (IConfiguration)configuration.GetSection("KioskConfigurationSettings"));
            ServiceCollectionServiceExtensions.AddSingleton<IOptionsKioskConfiguration, OptionsKioskConfiguration>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<IOptionsSnapshotKioskConfiguration, OptionsSnapshotKioskConfiguration>(serviceCollection);
            ServiceCollectionServiceExtensions.AddSingleton<IOptionsMonitorKioskConfiguration, OptionsMonitorKioskConfiguration>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<IConfigurationService, ConfigurationService>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<IConfigurationServiceJob, ConfigurationServiceJob>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<IConfigurationFileMissingJob, ConfigurationFileMissingJob>(serviceCollection);
            ServiceCollectionServiceExtensions.AddScoped<IConfigurationServiceUpdateStatusJob, ConfigurationServiceUpdateStatusJob>(serviceCollection);
            return serviceCollection;
        }
    }
}
