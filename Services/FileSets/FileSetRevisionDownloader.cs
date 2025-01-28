using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using UpdateClientService.API.Services.DownloadService;
using UpdateClientService.API.Services.FileCache;

namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetRevisionDownloader : IFileSetRevisionDownloader
    {
        private IFileCacheService _fileCacheService;
        private IDownloadService _downloadService;
        private readonly IZipDownloadHelper _zipHelper;
        private readonly AppSettings _settings;
        private readonly IDownloader _downloader;

        public FileSetRevisionDownloader(
          IFileCacheService fileCacheService,
          IDownloadService downloadService,
          IZipDownloadHelper zipHelper,
          IDownloader downloaderInterface,
          IOptionsMonitor<AppSettings> settings)
        {
            this._fileCacheService = fileCacheService;
            this._downloadService = downloadService;
            this._zipHelper = zipHelper;
            this._downloader = downloaderInterface;
            this._settings = settings.CurrentValue;
        }

        public bool DoesRevisionExist(RevisionChangeSetKey revisionChangeSetKey)
        {
            return this._fileCacheService.DoesRevisionExist(revisionChangeSetKey);
        }

        public bool IsDownloadError(DownloadData downloadData)
        {
            return downloadData != null && downloadData.DownloadState == DownloadState.Error;
        }

        public bool IsDownloadComplete(
          RevisionChangeSetKey revisionChangeSetKey,
          DownloadData downloadData)
        {
            if (this.DoesRevisionExist(revisionChangeSetKey))
                return true;
            return downloadData != null && downloadData.DownloadState == DownloadState.PostDownload;
        }

        public async Task<DownloadData> AddDownload(
          RevisionChangeSetKey revisionChangeSetKey,
          string hash,
          string path,
          DownloadPriority downloadPriority)
        {
            (bool flag, DownloadData downloadData) = await this.AddDownload(DownloadData.GetRevisionKey(revisionChangeSetKey), hash, this.GetRevisionUrl(path), downloadPriority);
            return flag ? downloadData : (DownloadData)null;
        }

        private async Task<(bool success, DownloadData downloadData)> AddDownload(
          string key,
          string hash,
          string url,
          DownloadPriority downloadPriority)
        {
            return await this._downloadService.AddRetrieveDownload(key, hash, url, downloadPriority, false);
        }

        public bool CompleteDownload(
          RevisionChangeSetKey revisionChangeSetKey,
          DownloadData downloadData)
        {
            bool flag = this._zipHelper.Extract(downloadData.Path, revisionChangeSetKey);
            if (flag)
                this._downloader.Complete(downloadData);
            return flag;
        }

        private string GetRevisionUrl(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            return new Uri(string.Format("{0}/{1}/{2}/{3}", (object)this._settings.BaseServiceUrl, (object)"api/downloads/s3/proxy", (object)Uri.EscapeDataString(path), (object)DownloadPathType.None)).ToString();
        }
    }
}
