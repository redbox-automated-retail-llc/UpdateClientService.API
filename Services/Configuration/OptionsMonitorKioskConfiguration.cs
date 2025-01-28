using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UpdateClientService.API.Services.IoT.Certificate.Security;

namespace UpdateClientService.API.Services.Configuration
{
    public class OptionsMonitorKioskConfiguration : BaseKioskConfiguration, IOptionsMonitorKioskConfiguration, IKioskConfiguration
    {
        public OptionsMonitorKioskConfiguration(ILogger<OptionsMonitorKioskConfiguration> logger, IOptionsMonitor<KioskConfigurationSettings> optionsMonitorKioskConfigurationSettings, IEncryptionService encryptionService)
            : base(logger, optionsMonitorKioskConfigurationSettings, encryptionService)
        {
        }
    }
}
