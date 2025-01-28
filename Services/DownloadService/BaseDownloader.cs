using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Threading.Tasks;
using UpdateClientService.API.App;

namespace UpdateClientService.API.Services.DownloadService
{
    public class BaseDownloader : IDownloader, IPersistentData
    {
        private readonly IPersistentDataCacheService _persistentDataCacheService;
        private readonly ILogger<BaseDownloader> _logger;

        public BaseDownloader(IPersistentDataCacheService cache, ILogger<BaseDownloader> logger)
        {
            this._logger = logger;
            this._persistentDataCacheService = cache;
        }

        public virtual async Task<bool> ProcessDownload(DownloadData downloadData) => true;

        public virtual async Task<bool> SaveDownload(DownloadData downloadData)
        {
            bool flag;
            try
            {
                flag = await this._persistentDataCacheService.Write<DownloadData>(downloadData, downloadData.FileName, UpdateClientServiceConstants.DownloadDataFolder);
                if (!flag)
                {
                    ILogger<BaseDownloader> logger = this._logger;
                    DownloadData downloadData1 = downloadData;
                    string str = "Unable to save DownloadData " + (downloadData1 != null ? downloadData1.ToJson() : (string)null);
                    this._logger.LogErrorWithSource(str, nameof(SaveDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/BaseDownloader.cs");
                }
            }
            catch (Exception ex)
            {
                ILogger<BaseDownloader> logger = this._logger;
                Exception exception = ex;
                DownloadData downloadData2 = downloadData;
                string str = "Exception while saving DownloadData " + (downloadData2 != null ? downloadData2.ToJson() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(SaveDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/BaseDownloader.cs");
                flag = false;
            }
            return flag;
        }

        public virtual async Task<bool> DeleteDownload(DownloadData downloadData)
        {
            bool flag;
            try
            {
                flag = await this._persistentDataCacheService.Delete(downloadData.FileName, UpdateClientServiceConstants.DownloadDataFolder);
                if (!flag)
                    this._logger.LogErrorWithSource("Unable to delete DownloadData with key: " + downloadData?.Key, nameof(DeleteDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/BaseDownloader.cs");
            }
            catch (Exception ex)
            {
                flag = false;
                this._logger.LogErrorWithSource(ex, "Exception while deleting DownloadData with key " + downloadData?.Key, nameof(DeleteDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/BaseDownloader.cs");
            }
            return flag;
        }

        public virtual async Task<bool> Complete(DownloadData downloadData)
        {
            bool success = true;
            try
            {
                downloadData.DownloadState = DownloadState.Complete;
                this._logger.LogInfoWithSource(string.Format("Setting DownloadState = {0} for DownloadData {1}", (object)downloadData.DownloadState, (object)downloadData.FileName), nameof(Complete), "/sln/src/UpdateClientService.API/Services/DownloadService/BaseDownloader.cs");
                int num = await this.SaveDownload(downloadData) ? 1 : 0;
            }
            catch (Exception ex)
            {
                success = false;
                this._logger.LogErrorWithSource(ex, "Exception while completing DownloadData with key: " + downloadData.Key, nameof(Complete), "/sln/src/UpdateClientService.API/Services/DownloadService/BaseDownloader.cs");
            }
            return success;
        }

        public virtual bool Cleanup(DownloadDataList downloadDataList) => true;
    }
}
