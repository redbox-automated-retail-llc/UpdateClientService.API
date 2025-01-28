using Newtonsoft.Json;

namespace UpdateClientService.API.Services.Configuration
{
    public class BaseCategorySetting
    {
        [JsonIgnore]
        public IKioskConfiguration ParentKioskConfiguration { get; set; }
    }
}
