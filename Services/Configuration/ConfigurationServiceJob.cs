using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.Configuration
{
    public class ConfigurationServiceJob : IConfigurationServiceJob, IInvocable
    {
        private IConfigurationService _configurationService;
        private ILogger<ConfigurationServiceJob> _logger;

        public ConfigurationServiceJob(
          ILogger<ConfigurationServiceJob> logger,
          IConfigurationService configurationService)
        {
            this._configurationService = configurationService;
            this._logger = logger;
        }

        public async Task Invoke()
        {
            this._logger.LogInfoWithSource("Invoking ConfigurationService.GetKioskConfigurationSettingChanges", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationServiceJob.cs");
            ApiBaseResponse configurationSettingChanges = await this._configurationService.GetKioskConfigurationSettingChanges(new long?());
        }
    }
}
