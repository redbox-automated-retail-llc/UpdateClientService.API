namespace UpdateClientService.API.Services.Configuration
{
    public interface IKioskConfiguration
    {
        long ConfigurationVersion { get; }

        void Log(ConfigLogParameters loggingParameters = null);

        Operations Operations { get; }

        FileSets FileSets { get; }

        KioskHealthConfiguration KioskHealth { get; }

        KioskClientProxyApi KioskClientProxyApi { get; }
    }
}
