using System.Threading.Tasks;

namespace UpdateClientService.API.Services.DownloadService
{
    public interface IDownloader : IPersistentData
    {
        Task<bool> ProcessDownload(DownloadData downloadData);

        Task<bool> SaveDownload(DownloadData downloadData);

        Task<bool> DeleteDownload(DownloadData downloadData);

        Task<bool> Complete(DownloadData downloadData);

        bool Cleanup(DownloadDataList downloadDataList);
    }
}
