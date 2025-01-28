namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetSettings
    {
        public int KioskEngineShutdownTimeoutMs { get; set; } = 15000;

        public int KioskEngineShutdownRetries { get; set; } = 2;
    }
}
