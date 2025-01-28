using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UpdateClientService.API.Services.IoT.Certificate.Security;

namespace UpdateClientService.API.Services.Configuration
{
    public class OptionsKioskConfiguration : BaseKioskConfiguration, IOptionsKioskConfiguration, IKioskConfiguration
    {
        public OptionsKioskConfiguration(ILogger<OptionsKioskConfiguration> logger, IOptions<KioskConfigurationSettings> optionsKioskConfigurationSettings, IEncryptionService encryptionService)
            : base(logger, optionsKioskConfigurationSettings, encryptionService)
        {
        }
    }
}
