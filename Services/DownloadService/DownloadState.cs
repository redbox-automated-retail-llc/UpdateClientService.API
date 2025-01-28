namespace UpdateClientService.API.Services.DownloadService
{
    public enum DownloadState
    {
        None,
        Error,
        Downloading,
        PostDownload,
        Complete,
    }
}
