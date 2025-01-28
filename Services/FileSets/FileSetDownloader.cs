using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UpdateClientService.API.Services.DownloadService;
using UpdateClientService.API.Services.FileCache;

namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetDownloader : IFileSetDownloader
    {
        private readonly IFileCacheService _fileCacheService;
        private readonly IDownloadService _downloadService;
        private readonly IZipDownloadHelper _zipHelper;
        private readonly IDownloader _downloader;
        private readonly AppSettings _settings;
        private readonly ILogger<FileSetDownloader> _logger;

        public FileSetDownloader(
          IFileCacheService fileCacheService,
          IDownloadService downloadService,
          IZipDownloadHelper zipHelper,
          IDownloader downloaderInterface,
          IOptionsMonitor<AppSettings> settings,
          ILogger<FileSetDownloader> logger)
        {
            this._fileCacheService = fileCacheService;
            this._downloadService = downloadService;
            this._zipHelper = zipHelper;
            this._downloader = downloaderInterface;
            this._settings = settings.CurrentValue;
            this._logger = logger;
        }

        public bool IsDownloaded(ClientFileSetRevision clientFileSetRevision)
        {
            List<FileSetFileInfo> fileSetInfoList = this.GetFileSetInfoList(clientFileSetRevision);
            return this.GetDownloadMethod(clientFileSetRevision, fileSetInfoList) == DownloadMethod.None;
        }

        public async Task<bool> DownloadFileSet(
          ClientFileSetRevision revision,
          DownloadDataList downloadDataList,
          DownloadPriority priority)
        {
            bool flag = true;
            try
            {
                List<FileSetFileInfo> fileSetInfoList = this.GetFileSetInfoList(revision);
                DownloadMethod downloadMethod = this.GetDownloadMethod(revision, fileSetInfoList);
                this._logger.LogInfoWithSource(string.Format("Download method is {0} for {1}", (object)downloadMethod, (object)revision.IdentifyingText()), nameof(DownloadFileSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetDownloader.cs");
                switch (downloadMethod)
                {
                    case DownloadMethod.FileSet:
                        flag = await this.DownloadByFileSet(revision, downloadDataList, priority);
                        break;
                    case DownloadMethod.PatchSet:
                        flag = await this.DownloadByPatchSet(revision, fileSetInfoList, downloadDataList, priority);
                        break;
                    case DownloadMethod.Files:
                        flag = await this.DownloadByFiles(fileSetInfoList, downloadDataList, priority);
                        break;
                    case DownloadMethod.Patches:
                        flag = await this.DownloadByPatches(fileSetInfoList, downloadDataList, priority);
                        break;
                }
            }
            catch (Exception ex)
            {
                ILogger<FileSetDownloader> logger = this._logger;
                Exception exception = ex;
                ClientFileSetRevision revisionChangeSetKey = revision;
                string str = "Exception while downloading file set " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(DownloadFileSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetDownloader.cs");
                flag = false;
            }
            return flag;
        }

        public bool CompleteDownloads(
          ClientFileSetRevision clientFileSetRevision,
          DownloadDataList downloadDataList)
        {
            bool flag = true;
            foreach (DownloadData downloadData in downloadDataList.Where<DownloadData>((Func<DownloadData, bool>)(x => x.DownloadState == DownloadState.PostDownload)))
            {
                if (this._zipHelper.Extract(downloadData.Path, (RevisionChangeSetKey)clientFileSetRevision))
                    this._downloader.Complete(downloadData);
                else
                    flag = false;
            }
            return flag;
        }

        private List<FileSetFileInfo> GetFileSetInfoList(ClientFileSetRevision clientFileSetRevision)
        {
            List<FileSetFileInfo> fileSetInfoList = new List<FileSetFileInfo>();
            foreach (ClientFileSetFile file in clientFileSetRevision.Files)
            {
                ClientFileSetFile eachClientFileSetFile = file;
                FileSetFileInfo fileSetFileInfo = new FileSetFileInfo()
                {
                    FileId = eachClientFileSetFile.FileId,
                    FileRevisionId = eachClientFileSetFile.FileRevisionId,
                    Key = this.GetFileKey(clientFileSetRevision.FileSetId, eachClientFileSetFile.FileId, eachClientFileSetFile.FileRevisionId),
                    FileSize = eachClientFileSetFile.FileSize,
                    PatchSize = 0,
                    FileHash = eachClientFileSetFile.FileHash
                };
                fileSetFileInfo.Exists = this._fileCacheService.DoesFileExist(clientFileSetRevision.FileSetId, eachClientFileSetFile.FileId, eachClientFileSetFile.FileRevisionId);
                if (fileSetFileInfo.Exists && !this._fileCacheService.IsFileHashValid(clientFileSetRevision.FileSetId, eachClientFileSetFile.FileId, eachClientFileSetFile.FileRevisionId, eachClientFileSetFile.ContentHash))
                {
                    this._logger.LogWarning(string.Format("Invalid Hash for file with FileSetId {0}, FileId {1}, FileRevisionId {2}", (object)clientFileSetRevision.FileSetId, (object)eachClientFileSetFile.FileId, (object)eachClientFileSetFile.FileRevisionId), Array.Empty<object>());
                    fileSetFileInfo.Exists = false;
                    this._fileCacheService.DeleteFile(clientFileSetRevision.FileSetId, eachClientFileSetFile.FileId, eachClientFileSetFile.FileRevisionId, true);
                }
                fileSetFileInfo.Url = this.GetFileUrl(eachClientFileSetFile, clientFileSetRevision.FileSetId, eachClientFileSetFile.FileRevisionId);
                if (!fileSetFileInfo.Exists)
                {
                    ClientPatchFileSetFile patchFileSetFile = clientFileSetRevision.PatchFiles.FirstOrDefault<ClientPatchFileSetFile>((Func<ClientPatchFileSetFile, bool>)(pf => pf.FileId == eachClientFileSetFile.FileId));
                    if (patchFileSetFile != null)
                    {
                        string fileKey = this.GetFileKey(clientFileSetRevision.FileSetId, eachClientFileSetFile.FileId, patchFileSetFile.PatchFileRevisionId);
                        bool flag = this._fileCacheService.DoesFileExist(clientFileSetRevision.FileSetId, eachClientFileSetFile.FileId, patchFileSetFile.PatchFileRevisionId);
                        if (flag && !this._fileCacheService.IsFileHashValid(clientFileSetRevision.FileSetId, eachClientFileSetFile.FileId, patchFileSetFile.PatchFileRevisionId, patchFileSetFile.ContentHash))
                        {
                            this._logger.LogWarning(string.Format("Invalid Hash for file with FileSetId {0}, FileId {1}, FileRevisionId {2}", (object)clientFileSetRevision.FileSetId, (object)eachClientFileSetFile.FileId, (object)patchFileSetFile.PatchFileRevisionId), Array.Empty<object>());
                            flag = false;
                            this._fileCacheService.DeleteFile(clientFileSetRevision.FileSetId, eachClientFileSetFile.FileId, patchFileSetFile.PatchFileRevisionId, true);
                        }
                        if (flag)
                        {
                            fileSetFileInfo.PatchSize = patchFileSetFile.FileSize;
                            fileSetFileInfo.PatchUrl = this.GetFileUrl(eachClientFileSetFile, clientFileSetRevision.FileSetId, eachClientFileSetFile.FileRevisionId, patchFileSetFile.PatchFileRevisionId);
                            fileSetFileInfo.PatchKey = fileKey;
                            fileSetFileInfo.PatchHash = patchFileSetFile.FileHash;
                        }
                    }
                }
                fileSetInfoList.Add(fileSetFileInfo);
            }
            return fileSetInfoList;
        }

        private DownloadMethod GetDownloadMethod(
          ClientFileSetRevision clientFileSetRevision,
          List<FileSetFileInfo> fileSetInfoList)
        {
            long num1 = fileSetInfoList.Sum<FileSetFileInfo>((Func<FileSetFileInfo, long>)(x => !x.Exists ? x.FileSize : 0L));
            if (num1 == 0L)
                return DownloadMethod.None;
            int num2 = fileSetInfoList.Count<FileSetFileInfo>((Func<FileSetFileInfo, bool>)(x => !x.Exists));
            List<FileSetDownloader.DownloadMethodSize> source = new List<FileSetDownloader.DownloadMethodSize>();
            source.Add(new FileSetDownloader.DownloadMethodSize()
            {
                DownloadMethod = DownloadMethod.Files,
                Size = num1,
                NumberOfDownloads = num2
            });
            source.Add(new FileSetDownloader.DownloadMethodSize()
            {
                DownloadMethod = DownloadMethod.FileSet,
                Size = clientFileSetRevision.SetFileSize,
                NumberOfDownloads = 1
            });
            if (clientFileSetRevision.PatchSetFileSize > 0L)
            {
                long num3 = fileSetInfoList.Sum<FileSetFileInfo>((Func<FileSetFileInfo, long>)(x =>
                {
                    if (x.Exists)
                        return 0;
                    return x.PatchSize <= 0L ? x.FileSize : x.PatchSize;
                }));
                source.Add(new FileSetDownloader.DownloadMethodSize()
                {
                    DownloadMethod = DownloadMethod.Patches,
                    Size = num3,
                    NumberOfDownloads = num2
                });
                int num4 = fileSetInfoList.Count<FileSetFileInfo>((Func<FileSetFileInfo, bool>)(x => !x.Exists || x.PatchSize == 0L));
                long num5 = fileSetInfoList.Sum<FileSetFileInfo>((Func<FileSetFileInfo, long>)(x => !x.Exists && x.PatchSize <= 0L ? x.FileSize : 0L)) + clientFileSetRevision.PatchSetFileSize;
                source.Add(new FileSetDownloader.DownloadMethodSize()
                {
                    DownloadMethod = DownloadMethod.PatchSet,
                    Size = num5,
                    NumberOfDownloads = num4 + 1
                });
            }
            return source.OrderBy<FileSetDownloader.DownloadMethodSize, long>((Func<FileSetDownloader.DownloadMethodSize, long>)(x => x.Size)).ThenBy<FileSetDownloader.DownloadMethodSize, int>((Func<FileSetDownloader.DownloadMethodSize, int>)(x => x.NumberOfDownloads)).FirstOrDefault<FileSetDownloader.DownloadMethodSize>().DownloadMethod;
        }

        private async Task<bool> DownloadByFileSet(
          ClientFileSetRevision clientFileSetRevision,
          DownloadDataList downloadDataList,
          DownloadPriority priority)
        {
            bool result = false;
            try
            {
                string key = this.GetRevisionSetKey(clientFileSetRevision);
                string fileSetUrl = this.GetFileSetUrl(clientFileSetRevision);
                if (!downloadDataList.ExistsByKey(key))
                {
                    (bool flag, DownloadData downloadData) = await this.AddDownload(key, clientFileSetRevision.SetFileHash, fileSetUrl, priority);
                    if (flag)
                    {
                        downloadDataList.Add(downloadData);
                        result = true;
                    }
                    else
                        this._logger.LogErrorWithSource("Unable to add DownloadData for " + key, nameof(DownloadByFileSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetDownloader.cs");
                }
                else
                    result = true;
                key = (string)null;
            }
            catch (Exception ex)
            {
                ILogger<FileSetDownloader> logger = this._logger;
                Exception exception = ex;
                ClientFileSetRevision revisionChangeSetKey = clientFileSetRevision;
                string str = "Excepton while adding DownloadData by FileSet for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(DownloadByFileSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetDownloader.cs");
            }
            return result;
        }

        private async Task<bool> DownloadByPatchSet(
          ClientFileSetRevision clientFileSetRevision,
          List<FileSetFileInfo> fileSetInfoList,
          DownloadDataList downloadDataList,
          DownloadPriority priority)
        {
            bool result = false;
            try
            {
                string revisionPatchSetKey = this.GetRevisionPatchSetKey(clientFileSetRevision);
                string patchSetFileUrl = this.GetPatchSetFileUrl(clientFileSetRevision);
                if (!downloadDataList.ExistsByKey(revisionPatchSetKey))
                {
                    (bool flag, DownloadData downloadData) = await this.AddDownload(revisionPatchSetKey, clientFileSetRevision.PatchSetFileHash, patchSetFileUrl, priority);
                    if (flag)
                    {
                        downloadDataList.Add(downloadData);
                        result = await this.AddDownloadsForNewFilesInPatchSet(fileSetInfoList, downloadDataList, priority);
                    }
                }
            }
            catch (Exception ex)
            {
                ILogger<FileSetDownloader> logger = this._logger;
                Exception exception = ex;
                ClientFileSetRevision revisionChangeSetKey = clientFileSetRevision;
                string str = "Exception while adding DownloadData for Patch Set with " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(DownloadByPatchSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetDownloader.cs");
            }
            return result;
        }

        private async Task<bool> AddDownloadsForNewFilesInPatchSet(
          List<FileSetFileInfo> fileSetInfoList,
          DownloadDataList downloadDataList,
          DownloadPriority priority)
        {
            bool result = true;
            foreach (FileSetFileInfo fileSetFileInfo in fileSetInfoList.Where<FileSetFileInfo>((Func<FileSetFileInfo, bool>)(x => !x.Exists && x.PatchSize == 0L)))
            {
                if (!downloadDataList.ExistsByKey(fileSetFileInfo.Key))
                {
                    (bool flag, DownloadData downloadData) = await this.AddDownload(fileSetFileInfo.Key, fileSetFileInfo.FileHash, fileSetFileInfo.Url, priority);
                    if (flag)
                        downloadDataList.Add(downloadData);
                    else
                        result = false;
                }
            }
            return result;
        }

        private async Task<bool> DownloadByPatches(
          List<FileSetFileInfo> fileSetInfoList,
          DownloadDataList downloadDataList,
          DownloadPriority priority)
        {
            bool result = true;
            try
            {
                foreach (FileSetFileInfo fileSetFileInfo in fileSetInfoList.Where<FileSetFileInfo>((Func<FileSetFileInfo, bool>)(x => !x.Exists)))
                {
                    bool flag1 = fileSetFileInfo.PatchSize > 0L;
                    string key = flag1 ? fileSetFileInfo.PatchKey : fileSetFileInfo.Key;
                    if (!downloadDataList.ExistsByKey(key))
                    {
                        string hash = flag1 ? fileSetFileInfo.PatchHash : fileSetFileInfo.FileHash;
                        string url = flag1 ? fileSetFileInfo.PatchUrl : fileSetFileInfo.Url;
                        (bool flag2, DownloadData downloadData) = await this.AddDownload(key, hash, url, priority);
                        if (flag2)
                            downloadDataList.Add(downloadData);
                        else
                            result = false;
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while adding DownloadData for Patch files", nameof(DownloadByPatches), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetDownloader.cs");
                result = false;
            }
            return result;
        }

        private async Task<bool> DownloadByFiles(
          List<FileSetFileInfo> fileSetInfoList,
          DownloadDataList downloadDataList,
          DownloadPriority downloadPriority)
        {
            bool result = true;
            try
            {
                foreach (FileSetFileInfo fileSetFileInfo in fileSetInfoList.Where<FileSetFileInfo>((Func<FileSetFileInfo, bool>)(x => !x.Exists)))
                {
                    if (!downloadDataList.ExistsByKey(fileSetFileInfo.Key))
                    {
                        (bool flag, DownloadData downloadData) = await this.AddDownload(fileSetFileInfo.Key, fileSetFileInfo.FileHash, fileSetFileInfo.Url, downloadPriority);
                        if (flag)
                            downloadDataList.Add(downloadData);
                        else
                            result = false;
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while adding DownloadData for files", nameof(DownloadByFiles), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetDownloader.cs");
                result = false;
            }
            return result;
        }

        private async Task<(bool success, DownloadData downloadData)> AddDownload(
          string key,
          string hash,
          string url,
          DownloadPriority downloadPriority)
        {
            return await this._downloadService.AddRetrieveDownload(key, hash, url, downloadPriority, false);
        }

        private string GetFileKey(long fileSetId, long fileId, long fileRevisionId)
        {
            return string.Format("{0},{1},{2},{3}", (object)2, (object)fileSetId, (object)fileId, (object)fileRevisionId);
        }

        private string GetFileUrl(
          ClientFileSetFile file,
          long fileSetId,
          long fileRevisionId,
          long patchFileRevisionId = 0)
        {
            return new Uri(string.Format("{0}/{1}/{2}/{3}", (object)this._settings.BaseServiceUrl, (object)"api/downloads/s3/proxy", (object)Uri.EscapeDataString(string.Format("filesets/{0}/File/{1}-{2}-{3}.zip", (object)fileSetId, (object)file.FileId, (object)fileRevisionId, (object)patchFileRevisionId)), (object)DownloadPathType.None)).ToString();
        }

        private string GetFilePatchKey(
          long fileSetId,
          long fileId,
          long fileRevisionId,
          long patchFileRevisionId)
        {
            return string.Format("{0},{1},{2},{3},{4}", (object)3, (object)fileSetId, (object)fileId, (object)fileRevisionId, (object)patchFileRevisionId);
        }

        private string GetRevisionSetKey(ClientFileSetRevision revision)
        {
            return string.Format("{0},{1},{2}", (object)4, (object)revision.FileSetId, (object)revision.RevisionId);
        }

        private string GetRevisionPatchSetKey(ClientFileSetRevision revision)
        {
            return string.Format("{0},{1},{2}", (object)5, (object)revision.FileSetId, (object)revision.RevisionId);
        }

        private string GetPatchSetFileUrl(ClientFileSetRevision revision)
        {
            return this.GetProxiedS3Url(revision.PatchSetPath);
        }

        private string GetFileSetUrl(ClientFileSetRevision revision)
        {
            return this.GetProxiedS3Url(revision.SetPath);
        }

        private string GetProxiedS3Url(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            return new Uri(string.Format("{0}/{1}/{2}/{3}", (object)this._settings.BaseServiceUrl, (object)"api/downloads/s3/proxy", (object)Uri.EscapeDataString(path), (object)DownloadPathType.None)).ToString();
        }

        private class DownloadMethodSize
        {
            public DownloadMethod DownloadMethod { get; set; }

            public long Size { get; set; }

            public int NumberOfDownloads { get; set; }
        }
    }
}
