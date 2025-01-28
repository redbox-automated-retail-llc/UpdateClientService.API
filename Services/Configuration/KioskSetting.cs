using System.Collections.Generic;

namespace UpdateClientService.API.Services.Configuration
{
    public class KioskSetting
    {
        public string Name { get; set; }

        public long SettingId { get; set; }

        public string DataType { get; set; }

        public List<KioskSettingValue> SettingValues { get; set; } = new List<KioskSettingValue>();
    }
}
