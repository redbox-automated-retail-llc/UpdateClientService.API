namespace UpdateClientService.API.Services.Configuration
{
    [Category(Name = "KioskHealth")]
    public class KioskHealthConfiguration : BaseCategorySetting
    {
        public bool ReportFailedPingsEnabled { get; set; } = true;
    }
}
