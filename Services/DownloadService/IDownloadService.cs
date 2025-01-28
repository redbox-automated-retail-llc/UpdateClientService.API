using Coravel.Invocable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UpdateClientService.API.Services.DownloadService.Responses;

namespace UpdateClientService.API.Services.DownloadService
{
    public interface IDownloadService : IInvocable
    {
        Task<DownloadDataList> GetDownloads(string pattern = null);

        Task<GetDownloadStatusesResponse> GetDownloadsResponse(string pattern = null);

        Task<GetDownloadStatusResponse> GetDownloadResponse(string key);

        Task<DownloadDataList> GetDownloads(Regex pattern);

        Task<GetFileResponse> AddDownload(
          string hash,
          string url,
          DownloadPriority priority,
          bool completeOnFinish = false);

        Task<bool> AddDownload(
          string key,
          string hash,
          string url,
          DownloadPriority priority,
          bool completeOnFinish = false);

        Task<(bool success, DownloadData downloadData)> AddRetrieveDownload(
          string key,
          string hash,
          string url,
          DownloadPriority priority,
          bool completeOnFinish);

        Task<DeleteDownloadResponse> CancelDownload(string key);

        Task<CompleteDownloadResponse> CompleteDownload(string key);

        Task<string> GetProxiedS3Url(string key, DownloadPathType type, bool isHead = false);
    }
}
