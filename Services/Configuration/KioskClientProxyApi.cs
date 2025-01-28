namespace UpdateClientService.API.Services.Configuration
{
    public class KioskClientProxyApi : BaseCategorySetting
    {
        public string ProxyApiUrl { get; set; }

        public int ProxyApiTimeout { get; set; } = 60000;

        [MaskLogValue(VisibleChars = 4)]
        public string ProxyApiKey { get; set; }
    }
}
