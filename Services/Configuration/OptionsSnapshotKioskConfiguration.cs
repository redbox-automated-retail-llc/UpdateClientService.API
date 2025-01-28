using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UpdateClientService.API.Services.IoT.Certificate.Security;

namespace UpdateClientService.API.Services.Configuration
{
    public class OptionsSnapshotKioskConfiguration : BaseKioskConfiguration, IOptionsSnapshotKioskConfiguration, IKioskConfiguration
    {
        public OptionsSnapshotKioskConfiguration(ILogger<OptionsSnapshotKioskConfiguration> logger, IOptionsSnapshot<KioskConfigurationSettings> optionsSnapshotKioskConfigurationSettings, IEncryptionService encryptionService)
            : base(logger, optionsSnapshotKioskConfigurationSettings, encryptionService)
        {
        }
    }
}
