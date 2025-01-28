using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.Configuration
{
    public class ConfigurationServiceUpdateStatusJob : IConfigurationServiceUpdateStatusJob, IInvocable
    {
        private IConfigurationService _configurationService;
        private ILogger<ConfigurationServiceUpdateStatusJob> _logger;

        public ConfigurationServiceUpdateStatusJob(
          ILogger<ConfigurationServiceUpdateStatusJob> logger,
          IConfigurationService configurationService)
        {
            this._configurationService = configurationService;
            this._logger = logger;
        }

        public async Task Invoke()
        {
            this._logger.LogInfoWithSource("Invoking ConfigurationService.UpdateConfigurationStatusIfNeeded", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationServiceUpdateStatusJob.cs");
            int num = await this._configurationService.UpdateConfigurationStatusIfNeeded() ? 1 : 0;
        }
    }
}
