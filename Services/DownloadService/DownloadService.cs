using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UpdateClientService.API.App;
using UpdateClientService.API.Services.DownloadService.Responses;
using UpdateClientService.API.Services.IoT.Commands;
using UpdateClientService.API.Services.IoT.IoTCommand;

namespace UpdateClientService.API.Services.DownloadService
{
    public class DownloadService : IDownloadService, IInvocable
    {
        private readonly ILogger<UpdateClientService.API.Services.DownloadService.DownloadService> _logger;
        private readonly IPersistentDataCacheService _cache;
        private readonly IDownloader _downloader;
        private readonly IIoTCommandClient _iotCommandClient;
        private readonly TimeSpan _proxiedS3UrlRequestTimeout = TimeSpan.FromSeconds(60.0);
        public const string DownloadExtension = ".dldat";

        private static SemaphoreSlim _lock => new SemaphoreSlim(1, 1);

        public DownloadService(
          ILogger<UpdateClientService.API.Services.DownloadService.DownloadService> logger,
          IPersistentDataCacheService cache,
          IDownloader downloader,
          IIoTCommandClient iotCommandClient)
        {
            this._logger = logger;
            this._cache = cache;
            this._downloader = downloader;
            this._iotCommandClient = iotCommandClient;
        }

        public async Task Invoke()
        {
            try
            {
                if (!await UpdateClientService.API.Services.DownloadService.DownloadService._lock.WaitAsync(0))
                {
                    this._logger.LogWarningWithSource("DownloadService is already being invoked. Skipping...", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                    return;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Could not acquire lock", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                return;
            }
            try
            {
                int num = await this.ProcessDownloads() ? 1 : 0;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "An unhandled exception occurred.", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
            }
            finally
            {
                SemaphoreSlim semaphoreSlim = UpdateClientService.API.Services.DownloadService.DownloadService._lock;
                if ((semaphoreSlim != null ? (semaphoreSlim.CurrentCount == 0 ? 1 : 0) : 0) != 0)
                    UpdateClientService.API.Services.DownloadService.DownloadService._lock.Release();
            }
        }

        public async Task<GetDownloadStatusesResponse> GetDownloadsResponse(string pattern = null)
        {
            try
            {
                GetDownloadStatusesResponse downloadsResponse = new GetDownloadStatusesResponse();
                GetDownloadStatusesResponse statusesResponse = downloadsResponse;
                statusesResponse.Statuses = (List<DownloadData>)await this.GetDownloads((Regex)null, pattern);
                return downloadsResponse;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while getting DownloadDatas for pattern " + pattern + ")", nameof(GetDownloadsResponse), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                GetDownloadStatusesResponse downloadsResponse = new GetDownloadStatusesResponse();
                downloadsResponse.StatusCode = HttpStatusCode.InternalServerError;
                return downloadsResponse;
            }
        }

        public async Task<GetDownloadStatusResponse> GetDownloadResponse(string key)
        {
            try
            {
                DownloadDataList downloads = await this.GetDownloads();
                DownloadData downloadData = downloads != null ? downloads.FirstOrDefault<DownloadData>((Func<DownloadData, bool>)(x => x.Key == key)) : (DownloadData)null;
                GetDownloadStatusResponse downloadResponse = new GetDownloadStatusResponse();
                downloadResponse.Status = downloadData;
                downloadResponse.StatusCode = downloadData != null ? HttpStatusCode.OK : HttpStatusCode.NotFound;
                return downloadResponse;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while getting DownloadData for key " + key + ")", nameof(GetDownloadResponse), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                GetDownloadStatusResponse downloadResponse = new GetDownloadStatusResponse();
                downloadResponse.StatusCode = HttpStatusCode.InternalServerError;
                return downloadResponse;
            }
        }

        public async Task<DownloadDataList> GetDownloads(string pattern = null)
        {
            return await this.GetDownloads((Regex)null, pattern);
        }

        public async Task<DownloadDataList> GetDownloads(Regex pattern)
        {
            return await this.GetDownloads(pattern, (string)null);
        }

        private async Task<DownloadDataList> GetDownloads(Regex regexPattern, string stringPattern)
        {
            DownloadDataList result = new DownloadDataList();
            try
            {
                List<PersistentDataWrapper<DownloadData>> source;
                if (regexPattern != null)
                    source = await this._cache.ReadLike<DownloadData>(regexPattern, UpdateClientServiceConstants.DownloadDataFolder, false);
                else
                    source = await this._cache.ReadLike<DownloadData>(string.IsNullOrWhiteSpace(stringPattern) ? DownloadDataConstants.DownloadExtension : stringPattern, UpdateClientServiceConstants.DownloadDataFolder, false);
                result.AddRange((IEnumerable<DownloadData>)source.Where<PersistentDataWrapper<DownloadData>>((Func<PersistentDataWrapper<DownloadData>, bool>)(d => d?.Data != null)).Select<PersistentDataWrapper<DownloadData>, DownloadData>((Func<PersistentDataWrapper<DownloadData>, DownloadData>)(d => d.Data)).ToList<DownloadData>());
            }
            catch (Exception ex)
            {
                this._logger.LogCriticalWithSource(ex, "An unhandled exception occurred", nameof(GetDownloads), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
            }
            return result;
        }

        public async Task<GetFileResponse> AddDownload(
          string hash,
          string url,
          DownloadPriority priority,
          bool completeOnFinish = false)
        {
            GetFileResponse response = new GetFileResponse();
            try
            {
                if (!await this.AddDownload(Guid.NewGuid().ToString(), hash, url, priority, completeOnFinish))
                    response.StatusCode = HttpStatusCode.InternalServerError;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while adding download for hash " + hash + ", url " + url, nameof(AddDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
            }
            return response;
        }

        public async Task<bool> AddDownload(
          string key,
          string hash,
          string url,
          DownloadPriority priority,
          bool completeOnFinish = false)
        {
            (bool flag, DownloadData _) = await this.AddRetrieveDownload(key, hash, url, priority, completeOnFinish);
            return flag;
        }

        public async Task<(bool success, DownloadData downloadData)> AddRetrieveDownload(
          string key,
          string hash,
          string url,
          DownloadPriority priority,
          bool completeOnFinish)
        {
            DownloadData downloadData = (DownloadData)null;
            this._logger.LogInfoWithSource(string.Format("Adding download key: {0} hash: {1} url: {2} priority: {3}", (object)key, (object)hash, (object)url, (object)priority), nameof(AddRetrieveDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
            bool flag;
            try
            {
                downloadData = DownloadData.Initialize(key, hash, url, priority, completeOnFinish);
                flag = await this._downloader.SaveDownload(downloadData);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while adding download for {0} hash: {1} url: {2} priority: {3}", (object)key, (object)hash, (object)url, (object)priority), nameof(AddRetrieveDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                flag = false;
            }
            return (flag, downloadData);
        }

        public async Task<DeleteDownloadResponse> CancelDownload(string key)
        {
            DeleteDownloadResponse response = new DeleteDownloadResponse();
            this._logger.LogInfoWithSource("Deleting DownloadData with key: " + key, nameof(CancelDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
            try
            {
                DownloadData downloadData = await this.GetDownloadData(key);
                bool flag = downloadData != null;
                if (flag)
                    flag = !await this._downloader.DeleteDownload(downloadData);
                if (flag)
                    response.StatusCode = HttpStatusCode.InternalServerError;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while canceling download with key " + key, nameof(CancelDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public async Task<CompleteDownloadResponse> CompleteDownload(string key)
        {
            CompleteDownloadResponse response = new CompleteDownloadResponse();
            this._logger.LogInfoWithSource("Completing download matching key: " + key, nameof(CompleteDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
            try
            {
                DownloadData downloadData = await this.GetDownloadData(key);
                if (downloadData != null)
                {
                    if (!await this._downloader.Complete(downloadData))
                        response.StatusCode = HttpStatusCode.InternalServerError;
                }
                else
                {
                    this._logger.LogErrorWithSource("No download found matching key: " + key, nameof(CompleteDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                    response.StatusCode = HttpStatusCode.InternalServerError;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while completing Download with key " + key, nameof(CompleteDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        public async Task<string> GetProxiedS3Url(string key, DownloadPathType type, bool isHead = false)
        {
            PerformIoTCommandParameters tcommandParameters = new PerformIoTCommandParameters()
            {
                RequestTimeout = new TimeSpan?(this._proxiedS3UrlRequestTimeout),
                WaitForResponse = true
            };
            if (!Debugger.IsAttached)
                tcommandParameters.IoTTopic = "$aws/rules/kioskrestcall";
            IIoTCommandClient iotCommandClient = this._iotCommandClient;
            IoTCommandModel request = new IoTCommandModel();
            request.Version = 2;
            request.Command = CommandEnum.GetPresignedS3Url;
            request.Payload = (object)new PresignedS3UrlRequest()
            {
                Key = key,
                Type = type,
                IsHead = isHead
            };
            PerformIoTCommandParameters parameters = tcommandParameters;
            string uriString = await iotCommandClient.PerformIoTCommandWithStringResult(request, parameters);
            if (string.IsNullOrEmpty(uriString) || !Uri.TryCreate(uriString, UriKind.Absolute, out Uri _))
            {
                this._logger.LogErrorWithSource("Url '" + uriString + "' is not a valid Url", nameof(GetProxiedS3Url), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                uriString = (string)null;
            }
            return uriString;
        }

        private async Task<DownloadData> GetDownloadData(string key)
        {
            return (await this.GetDownloads()).Where<DownloadData>((Func<DownloadData, bool>)(d => d.Key == key)).FirstOrDefault<DownloadData>();
        }

        private async Task<bool> ProcessDownloads()
        {
            bool result = true;
            try
            {
                DownloadDataList downloadDataList = await this.GetDownloads();
                DownloadDataList downloadDataList1 = downloadDataList;
                if ((downloadDataList1 != null ? ((downloadDataList1.Count) > 0 ? 1 : 0) : 0) != 0)
                    this._logger.LogInfoWithSource(string.Format("Start processing {0} downloads", (object)downloadDataList.Count), nameof(ProcessDownloads), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                foreach (DownloadData downloadData in (List<DownloadData>)downloadDataList)
                {
                    bool flag = result;
                    result = flag & await this._downloader.ProcessDownload(downloadData);
                }
                result &= this._downloader.Cleanup(downloadDataList);
                DownloadDataList downloadDataList2 = downloadDataList;
                if ((downloadDataList2 != null ? ((downloadDataList2.Count) > 0 ? 1 : 0) : 0) != 0)
                    this._logger.LogInfoWithSource("Finished executing", nameof(ProcessDownloads), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                downloadDataList = (DownloadDataList)null;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while processing downloads", nameof(ProcessDownloads), "/sln/src/UpdateClientService.API/Services/DownloadService/DownloadService.cs");
                result = false;
            }
            return result;
        }
    }
}
