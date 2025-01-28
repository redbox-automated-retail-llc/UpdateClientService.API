using System.Threading.Tasks;
using UpdateClientService.API.Services.DownloadService;

namespace UpdateClientService.API.Services.FileSets
{
    public interface IFileSetDownloader
    {
        bool IsDownloaded(ClientFileSetRevision revision);

        Task<bool> DownloadFileSet(
          ClientFileSetRevision revision,
          DownloadDataList downloads,
          DownloadPriority priority);

        bool CompleteDownloads(ClientFileSetRevision revision, DownloadDataList downloads);
    }
}
