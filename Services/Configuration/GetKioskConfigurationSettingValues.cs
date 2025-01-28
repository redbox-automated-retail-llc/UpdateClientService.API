using System.Collections.Generic;

namespace UpdateClientService.API.Services.Configuration
{
    public class GetKioskConfigurationSettingValues
    {
        public long KioskId { get; set; }

        public long ConfigurationVersion { get; set; }

        public string ConfigurationVersionHash { get; set; }

        public List<KioskConfigurationCategory> Categories { get; set; } = new List<KioskConfigurationCategory>();
    }
}
