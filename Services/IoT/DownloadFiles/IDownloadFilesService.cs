using System.Threading.Tasks;
using UpdateClientService.API.Services.DownloadService;
using UpdateClientService.API.Services.IoT.Commands.DownloadFiles;

namespace UpdateClientService.API.Services.IoT.DownloadFiles
{
    public interface IDownloadFilesService
    {
        Task HandleDownloadFileJob(DownloadFileJob job);

        Task HandleScheduledJobs();

        Task<DownloadDataList> GetDownloadFileJobStatus(string bitsJobId);

        Task CancelDownloadFileJob(DownloadFileJob job);
    }
}
