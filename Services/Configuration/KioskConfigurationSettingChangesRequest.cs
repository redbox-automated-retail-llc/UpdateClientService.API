namespace UpdateClientService.API.Services.Configuration
{
    public class KioskConfigurationSettingChangesRequest
    {
        public string SettingChangesRequestId { get; set; }

        public long KioskId { get; set; }

        public long CurrentConfigurationVersionId { get; set; }

        public string ConfigurationVersionHash { get; set; }

        public long? RequestedConfigurationVersionId { get; set; }

        public int? PageNumber { get; set; }
    }
}
