namespace UpdateClientService.API.Services.DownloadService
{
    public class PresignedS3UrlRequest
    {
        public string Key { get; set; }

        public DownloadPathType Type { get; set; }

        public bool IsHead { get; set; }
    }
}
