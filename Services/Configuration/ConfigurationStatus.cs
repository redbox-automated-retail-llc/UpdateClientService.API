namespace UpdateClientService.API.Services.Configuration
{
    public class ConfigurationStatus : IPersistentData
    {
        public bool EnableUpdates { get; set; } = true;
    }
}
