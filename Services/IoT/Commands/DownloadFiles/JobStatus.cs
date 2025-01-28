namespace UpdateClientService.API.Services.IoT.Commands.DownloadFiles
{
    public enum JobStatus
    {
        Created,
        InProgress,
        Scheduled,
        Complete,
        Error,
        CancelRequested,
        Canceled,
    }
}
