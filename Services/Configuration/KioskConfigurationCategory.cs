using System.Collections.Generic;

namespace UpdateClientService.API.Services.Configuration
{
    public class KioskConfigurationCategory
    {
        public string Name { get; set; }

        public List<KioskSetting> Settings { get; set; } = new List<KioskSetting>();
    }
}
