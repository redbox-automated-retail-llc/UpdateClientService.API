using System.Threading.Tasks;
using UpdateClientService.API.Services.DownloadService;

namespace UpdateClientService.API.Services.FileSets
{
    public interface IFileSetRevisionDownloader
    {
        bool DoesRevisionExist(RevisionChangeSetKey revisionChangeSetKey);

        bool IsDownloadError(DownloadData downloadData);

        bool IsDownloadComplete(RevisionChangeSetKey revisionChangeSetKey, DownloadData downloadData);

        Task<DownloadData> AddDownload(
          RevisionChangeSetKey revisionChangeSetKey,
          string hash,
          string path,
          DownloadPriority downloadPriority);

        bool CompleteDownload(RevisionChangeSetKey revisionChangeSetKey, DownloadData downloader);
    }
}
