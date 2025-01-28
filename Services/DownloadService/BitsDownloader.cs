using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Transfer;

namespace UpdateClientService.API.Services.DownloadService
{
    public class BitsDownloader : BaseDownloader
    {
        private readonly ILogger<BitsDownloader> _logger;
        private readonly ITransferService _transferService;
        private const string DownloadTypeName = "Bits downloader";
        private const string BitsNameFormat = "<UCS::~{0}~::UCS>";
        private const string BitsNameLeft = "<UCS::~";
        private const string BitsNameRight = "~::UCS>";

        public BitsDownloader(
          ILogger<BitsDownloader> logger,
          ITransferService transferService,
          IPersistentDataCacheService cache)
          : base(cache, (ILogger<BaseDownloader>)logger)
        {
            this._logger = logger;
            this._transferService = transferService;
        }

        public override async Task<bool> ProcessDownload(DownloadData downloadData)
        {
            BitsDownloader bitsDownloader = this;
            BitsDownloader.ProcessDownloadDataInfo processDownloadDataInfo = new BitsDownloader.ProcessDownloadDataInfo()
            {
                BitsDownloader = bitsDownloader,
                DownloadData = downloadData
            };
            bitsDownloader.SetBitTransferStatus(processDownloadDataInfo.DownloadData);
            await bitsDownloader.CheckNone(processDownloadDataInfo);
            await bitsDownloader.CheckDownloading(processDownloadDataInfo);
            await bitsDownloader.CheckPostDownload(processDownloadDataInfo);
            await bitsDownloader.CheckComplete(processDownloadDataInfo);
            await bitsDownloader.CheckError(processDownloadDataInfo);
            return processDownloadDataInfo.Success;
        }

        public override bool Cleanup(DownloadDataList downloadList)
        {
            bool flag;
            try
            {
                List<ITransferJob> jobs;
                if (!this.GetDownloadFileBitsJobs(out jobs))
                    return false;
                List<Error> source = new List<Error>();
                foreach (ITransferJob transferJob in jobs)
                {
                    if (downloadList.GetByBitsGuid(transferJob.ID) == null)
                    {
                        this._logger.LogInfoWithSource(string.Format("Clearing bits job, guid {0} that has no download file changeset.", (object)transferJob.ID), nameof(Cleanup), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
                        source.AddRange((IEnumerable<Error>)transferJob.Cancel());
                    }
                }
                flag = !source.Any<Error>();
            }
            catch (Exception ex)
            {
                flag = false;
                this._logger.LogErrorWithSource(ex, "Exception while cleaning up DownloadDataList", nameof(Cleanup), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            }
            return flag;
        }

        private void SetBitTransferStatus(DownloadData downloadData)
        {
            if (downloadData.BitsGuid == new Guid() || downloadData.DownloadState == DownloadState.Error)
                return;
            if (downloadData.DownloadState == DownloadState.PostDownload)
            {
                downloadData.BitsTransferred = downloadData.BitsTotal;
            }
            else
            {
                ITransferJob job;
                this._transferService.GetJob(downloadData.BitsGuid, out job);
                if (job == null)
                {
                    this._logger.LogInfoWithSource("Job was null for " + downloadData.ToJson(), nameof(SetBitTransferStatus), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
                }
                else
                {
                    try
                    {
                        downloadData.BitsTotal = job.TotalBytes == ulong.MaxValue ? long.MaxValue : Convert.ToInt64(job.TotalBytes);
                        downloadData.BitsTransferred = Convert.ToInt64(job.TotalBytesTransfered);
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogErrorWithSource(ex, "Something went wrong while updating transfer status.", nameof(SetBitTransferStatus), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
                    }
                }
            }
        }

        private TransferJobPriority GetJobPriority(DownloadData downloadData)
        {
            switch (downloadData.DownloadPriority)
            {
                case DownloadPriority.Normal:
                    return TransferJobPriority.Normal;
                case DownloadPriority.High:
                    return TransferJobPriority.High;
                case DownloadPriority.Foreground:
                    return TransferJobPriority.Foreground;
                default:
                    return TransferJobPriority.Low;
            }
        }

        private async Task CheckNone(
          BitsDownloader.ProcessDownloadDataInfo processDownloadDataInfo)
        {
            BitsDownloader bitsDownloader = this;
            DownloadData downloadData = processDownloadDataInfo.DownloadData;
            if (downloadData.DownloadState != DownloadState.None)
                return;
            this._logger.LogInfoWithSource(string.Format("{0} checking state: {1} for {2}", (object)"Bits downloader", (object)downloadData.DownloadState, (object)downloadData.Key), nameof(CheckNone), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            try
            {
                List<Error> source = new List<Error>();
                string name = string.Format("<UCS::~{0}~::UCS>", (object)downloadData.Key);
                ITransferJob job;
                source.AddRange((IEnumerable<Error>)bitsDownloader._transferService.CreateDownloadJob(name, out job));
                source.AddRange((IEnumerable<Error>)job.SetMinimumRetryDelay(60U));
                source.AddRange((IEnumerable<Error>)job.SetNoProgressTimeout(0U));
                source.AddRange((IEnumerable<Error>)job.SetPriority(bitsDownloader.GetJobPriority(downloadData)));
                source.AddRange((IEnumerable<Error>)job.AddItem(downloadData.Url, Path.GetTempFileName()));
                if (source.Any<Error>())
                {
                    processDownloadDataInfo.Success = false;
                    source.AddRange((IEnumerable<Error>)job.Cancel());
                    if (downloadData.RetryCount == 10)
                    {
                        downloadData.DownloadState = DownloadState.Error;
                        source.Add(new Error()
                        {
                            Message = string.Format("Retry limit exceeded for {0}", (object)job.ID)
                        });
                    }
                    else
                        downloadData.DownloadState = DownloadState.None;
                    downloadData.Message = string.Join(",", source.Where<Error>((Func<Error, bool>)(e => !string.IsNullOrWhiteSpace(e.Message))).Select<Error, string>((Func<Error, string>)(e => e.Message)));
                    ++downloadData.RetryCount;
                    int num = await bitsDownloader.SaveDownload(downloadData) ? 1 : 0;
                }
                else
                {
                    source.AddRange((IEnumerable<Error>)job.Resume());
                    if (source.Any<Error>())
                    {
                        int num1 = await processDownloadDataInfo.Update(new DownloadState?(DownloadState.Error), source[0].Message) ? 1 : 0;
                    }
                    else
                    {
                        downloadData.BitsGuid = job.ID;
                        int num2 = await processDownloadDataInfo.Update(new DownloadState?(DownloadState.Downloading)) ? 1 : 0;
                        this._logger.LogInfoWithSource(string.Format("Download Name: {0} has been put on the download queue. BITS job started with name: {1} and guid: {2}.", (object)downloadData.Key, (object)name, (object)job.ID), nameof(CheckNone), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
                        name = (string)null;
                        job = (ITransferJob)null;
                    }
                }
            }
            catch (Exception ex)
            {
                processDownloadDataInfo.Success = false;
                ILogger<BitsDownloader> logger = bitsDownloader._logger;
                Exception exception = ex;
                BitsDownloader.ProcessDownloadDataInfo downloadDataInfo = processDownloadDataInfo;
                string str1;
                if (downloadDataInfo == null)
                {
                    str1 = (string)null;
                }
                else
                {
                    DownloadData downloadData1 = downloadDataInfo.DownloadData;
                    str1 = downloadData1 != null ? downloadData1.ToJson() : (string)null;
                }
                string str2 = "Exception while checking status None for DownloadData " + str1;
                this._logger.LogErrorWithSource(exception, str2, nameof(CheckNone), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            }
        }

        private async Task<bool> UpdateDownloadData(
          DownloadData downloadData,
          DownloadState? downloadState,
          string errorMessage = null)
        {
            BitsDownloader bitsDownloader = this;
            if (downloadState.HasValue)
            {
                this._logger.LogInfoWithSource(string.Format("Setting DownloadState = {0} for DownloadData {1}", (object)downloadState, (object)downloadData.FileName), nameof(UpdateDownloadData), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
                downloadData.DownloadState = downloadState.Value;
            }
            if (errorMessage != null)
                downloadData.Message = errorMessage;
            return await bitsDownloader.SaveDownload(downloadData);
        }

        private async Task CheckDownloading(
          BitsDownloader.ProcessDownloadDataInfo processDownloadDataInfo)
        {
            DownloadData downloadData = processDownloadDataInfo.DownloadData;
            if (downloadData.DownloadState != DownloadState.Downloading)
                return;
            this._logger.LogInfoWithSource(string.Format("{0} checking state: {1} for {2}", (object)"Bits downloader", (object)downloadData.DownloadState, (object)downloadData.Key), nameof(CheckDownloading), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            try
            {
                List<ITransferJob> jobs;
                if (!this.GetDownloadFileBitsJobs(out jobs))
                {
                    int num1 = await processDownloadDataInfo.Update(new DownloadState?(DownloadState.Error), "Error getting Jobs list") ? 1 : 0;
                }
                else
                {
                    ITransferJob job = jobs.Where<ITransferJob>((Func<ITransferJob, bool>)(j => j.ID == downloadData.BitsGuid)).FirstOrDefault<ITransferJob>();
                    if (job == null)
                    {
                        int num2 = await processDownloadDataInfo.Update(new DownloadState?(DownloadState.Error), "Bits job missing for key: " + downloadData.Key) ? 1 : 0;
                    }
                    else if (job.Status == TransferStatus.Error)
                    {
                        job.Cancel();
                        int num3 = await processDownloadDataInfo.Update(new DownloadState?(DownloadState.Error), "Bits job error for key: " + downloadData.Key) ? 1 : 0;
                    }
                    else if (job.Status == TransferStatus.Suspended)
                    {
                        job.Cancel();
                        int num4 = await processDownloadDataInfo.Update(new DownloadState?(DownloadState.Error), "Bits job suspended for key: " + downloadData.Key) ? 1 : 0;
                    }
                    else
                    {
                        if (job.Status == TransferStatus.TransientError)
                        {
                            this._logger.LogInfoWithSource("BITS job status is TransientError for key: " + downloadData.Key, nameof(CheckDownloading), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
                            processDownloadDataInfo.Success = false;
                        }
                        if (job.Status != TransferStatus.Transferred)
                            return;
                        await this.FinishBitsDownload(job, processDownloadDataInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                processDownloadDataInfo.Success = false;
                ILogger<BitsDownloader> logger = this._logger;
                Exception exception = ex;
                DownloadState local = DownloadState.Downloading;
                BitsDownloader.ProcessDownloadDataInfo downloadDataInfo = processDownloadDataInfo;
                string str1;
                if (downloadDataInfo == null)
                {
                    str1 = (string)null;
                }
                else
                {
                    DownloadData downloadData1 = downloadDataInfo.DownloadData;
                    str1 = downloadData1 != null ? downloadData1.ToJson() : (string)null;
                }
                string str2 = string.Format("Exception while checking status {0} for DownloadData {1}", (object)local, (object)str1);
                this._logger.LogErrorWithSource(exception, str2, nameof(CheckDownloading), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            }
        }

        private async Task FinishBitsDownload(
          ITransferJob job,
          BitsDownloader.ProcessDownloadDataInfo processDownloadDataInfo)
        {
            DownloadData downloadData = processDownloadDataInfo.DownloadData;
            try
            {
                this._logger.LogInfoWithSource("Bits downloader FinishBitsDownload for " + downloadData.Key, nameof(FinishBitsDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
                List<Error> source = new List<Error>();
                List<ITransferItem> items;
                source.AddRange((IEnumerable<Error>)job.GetItems(out items));
                source.AddRange((IEnumerable<Error>)job.Complete());
                if (source.Any<Error>())
                {
                    job.Cancel();
                    int num = await processDownloadDataInfo.Update(new DownloadState?(DownloadState.Error), string.Format("BitsDownloader -> Job {0} get items or completion failed. This download will be canceled.", (object)job.ID)) ? 1 : 0;
                }
                else
                {
                    ITransferItem transferItem = items[0];
                    string downloadPath = Path.Combine(transferItem.Path, transferItem.Name);
                    string fileHash;
                    bool flag = this.CheckHash(downloadPath, downloadData.Hash, out fileHash);
                    if (!string.IsNullOrWhiteSpace(downloadData.Hash) && !flag)
                    {
                        int num = await processDownloadDataInfo.Update(new DownloadState?(DownloadState.Error), "BitsDownloader -> Download file: " + downloadData.Key + " path: " + downloadPath + " should have hash " + downloadData.Hash + " but it has hash " + fileHash + ". This download failed and will need to be restarted.") ? 1 : 0;
                        File.Delete(downloadPath);
                    }
                    else
                    {
                        downloadData.Path = downloadPath;
                        int num = await processDownloadDataInfo.Update(new DownloadState?(DownloadState.PostDownload)) ? 1 : 0;
                        if (string.IsNullOrWhiteSpace(downloadData.Hash))
                            this._logger.LogInfoWithSource("Download file: " + downloadData.Key + " was not provided with a hash to compare against. Ignoring check.", nameof(FinishBitsDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
                        downloadPath = (string)null;
                    }
                }
            }
            catch (Exception ex)
            {
                processDownloadDataInfo.Success = false;
                this._logger.LogErrorWithSource(ex, "Exception while finishing Bits download for " + downloadData.Key, nameof(FinishBitsDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            }
        }

        private async Task CheckPostDownload(
          BitsDownloader.ProcessDownloadDataInfo processDownloadDataInfo)
        {
            BitsDownloader bitsDownloader = this;
            DownloadData downloadData = processDownloadDataInfo.DownloadData;
            if (downloadData.DownloadState != DownloadState.PostDownload)
                return;
            this._logger.LogInfoWithSource(string.Format("{0} checking state: {1} for {2}", (object)"Bits downloader", (object)downloadData.DownloadState, (object)downloadData.Key), nameof(CheckPostDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            if (!downloadData.CompleteOnFinish)
                return;
            this._logger.LogInfoWithSource("CompleteOnFinish for " + downloadData.FileName, nameof(CheckPostDownload), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            BitsDownloader.ProcessDownloadDataInfo downloadDataInfo1 = processDownloadDataInfo;
            BitsDownloader.ProcessDownloadDataInfo downloadDataInfo = downloadDataInfo1;
            bool success = downloadDataInfo1.Success;
            downloadDataInfo.Success = success & await bitsDownloader.Complete(downloadData);
            downloadDataInfo = (BitsDownloader.ProcessDownloadDataInfo)null;
        }

        private async Task CheckComplete(
          BitsDownloader.ProcessDownloadDataInfo processDownloadDataInfo)
        {
            BitsDownloader bitsDownloader = this;
            DownloadData downloadData = processDownloadDataInfo.DownloadData;
            if (downloadData.DownloadState != DownloadState.Complete)
                return;
            this._logger.LogInfoWithSource(string.Format("{0} checking state: {1} for {2}", (object)"Bits downloader", (object)downloadData.DownloadState, (object)downloadData.Key), nameof(CheckComplete), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            BitsDownloader.ProcessDownloadDataInfo downloadDataInfo1 = processDownloadDataInfo;
            BitsDownloader.ProcessDownloadDataInfo downloadDataInfo = downloadDataInfo1;
            bool success = downloadDataInfo1.Success;
            downloadDataInfo.Success = success & await bitsDownloader.DeleteDownload(downloadData);
            downloadDataInfo = (BitsDownloader.ProcessDownloadDataInfo)null;
        }

        private async Task CheckError(
          BitsDownloader.ProcessDownloadDataInfo processDownloadDataInfo)
        {
            BitsDownloader bitsDownloader = this;
            DownloadData downloadData = processDownloadDataInfo.DownloadData;
            if (downloadData.DownloadState != DownloadState.Error)
                return;
            this._logger.LogInfoWithSource(string.Format("{0} checking state: {1} for {2}", (object)"Bits downloader", (object)downloadData.DownloadState, (object)downloadData.Key), nameof(CheckError), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            Guid bitsGuid = downloadData.BitsGuid;
            List<Error> source = new List<Error>();
            List<ITransferJob> jobs;
            if (bitsDownloader.GetDownloadFileBitsJobs(out jobs))
            {
                ITransferJob transferJob = jobs.Where<ITransferJob>((Func<ITransferJob, bool>)(j => j.ID == downloadData.BitsGuid)).FirstOrDefault<ITransferJob>();
                if (transferJob != null)
                {
                    source.AddRange((IEnumerable<Error>)transferJob.GetErrors());
                    string str = string.Join(", ", source.Select<Error, string>((Func<Error, string>)(x => x.Message)));
                    this._logger.LogWarningWithSource("Bits job error message for DownloadData " + downloadData.Key + ": " + str, nameof(CheckError), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
                    source.AddRange((IEnumerable<Error>)transferJob.Cancel());
                }
            }
            processDownloadDataInfo.Success &= !source.Any<Error>();
            BitsDownloader.ProcessDownloadDataInfo downloadDataInfo1 = processDownloadDataInfo;
            BitsDownloader.ProcessDownloadDataInfo downloadDataInfo = downloadDataInfo1;
            bool success = downloadDataInfo1.Success;
            downloadDataInfo.Success = success & await bitsDownloader.DeleteDownload(downloadData);
            downloadDataInfo = (BitsDownloader.ProcessDownloadDataInfo)null;
        }

        private bool GetDownloadFileBitsJobs(out List<ITransferJob> jobs)
        {
            List<Error> source = new List<Error>();
            source.AddRange((IEnumerable<Error>)this._transferService.GetJobs(out jobs, false));
            jobs = !source.Any<Error>() ? jobs.Where<ITransferJob>((Func<ITransferJob, bool>)(j => j.Name.StartsWith("<UCS::~") && j.Name.EndsWith("~::UCS>"))).ToList<ITransferJob>() : new List<ITransferJob>();
            return !source.Any<Error>();
        }

        private bool CheckHash(string filePath, string hash, out string fileHash)
        {
            bool flag = false;
            fileHash = string.Empty;
            try
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                {
                    fileHash = ((Stream)fileStream).GetSHA1Hash();
                    flag = fileHash == hash;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while Checking Hash for " + filePath, nameof(CheckHash), "/sln/src/UpdateClientService.API/Services/DownloadService/BitsDownloader.cs");
            }
            return flag;
        }

        private class ProcessDownloadDataInfo
        {
            public bool Success { get; set; } = true;

            public DownloadData DownloadData { get; set; }

            public BitsDownloader BitsDownloader { get; set; }

            public async Task<bool> Update(DownloadState? downloadState = null, string errorMessage = null)
            {
                this.Success &= string.IsNullOrEmpty(errorMessage);
                bool flag = await this.BitsDownloader.UpdateDownloadData(this.DownloadData, downloadState, errorMessage);
                this.Success &= flag;
                return flag;
            }
        }
    }
}
