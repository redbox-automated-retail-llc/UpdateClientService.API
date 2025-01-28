using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.Configuration
{
    public class ConfigurationFileMissingJob : IConfigurationFileMissingJob, IInvocable
    {
        private IConfigurationService _configurationService;
        private readonly IKioskConfiguration _kioskConfiguration;
        private ILogger<ConfigurationFileMissingJob> _logger;

        public ConfigurationFileMissingJob(
          ILogger<ConfigurationFileMissingJob> logger,
          IConfigurationService configurationService,
          IOptionsSnapshotKioskConfiguration kioskConfiguration)
        {
            this._configurationService = configurationService;
            this._kioskConfiguration = (IKioskConfiguration)kioskConfiguration;
            this._logger = logger;
        }

        public async Task Invoke()
        {
            if (this._kioskConfiguration.ConfigurationVersion != 0L)
                return;
            this._logger.LogWarningWithSource("KioskConfiguration is not current.  Getting current configuration version...", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/Configuration/ConfigurationFileMissingJob.cs");
            ApiBaseResponse configurationSettingChanges = await this._configurationService.GetKioskConfigurationSettingChanges(new long?());
        }
    }
}
