using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UpdateClientService.API.App;
using UpdateClientService.API.Services.DownloadService;
using UpdateClientService.API.Services.DownloadService.Responses;
using UpdateClientService.API.Services.IoT.Commands;
using UpdateClientService.API.Services.IoT.Commands.DownloadFiles;
using UpdateClientService.API.Services.IoT.IoTCommand;
using UpdateClientService.API.Services.Utilities;

namespace UpdateClientService.API.Services.IoT.DownloadFiles
{
    public class DownloadFilesService : IDownloadFilesService
    {
        private readonly IStoreService _store;
        private readonly IPersistentDataCacheService _cache;
        private readonly IIoTCommandClient _iotCommandClient;
        private readonly ILogger<DownloadFilesService> _logger;
        private readonly IDownloadService _downloadService;
        private readonly ICommandLineService _cmdService;
        private readonly AppSettings _settings;
        private readonly TimeSpan _proxiedS3UrlRequestTimeout = TimeSpan.FromSeconds(60.0);

        public DownloadFilesService(
          ILogger<DownloadFilesService> logger,
          IStoreService store,
          IPersistentDataCacheService cache,
          IIoTCommandClient iotCommandClient,
          IDownloadService downloadService,
          ICommandLineService cmdService,
          IOptionsMonitor<AppSettings> settings)
        {
            this._logger = logger;
            this._store = store;
            this._cache = cache;
            this._iotCommandClient = iotCommandClient;
            this._downloadService = downloadService;
            this._cmdService = cmdService;
            this._settings = settings.CurrentValue;
        }

        public async Task HandleDownloadFileJob(DownloadFileJob job)
        {
            DownloadFileJobExecution execution = new DownloadFileJobExecution()
            {
                DownloadFileJobId = job.DownloadFileJobId,
                KioskId = this._store.KioskId,
                Status = JobStatus.Scheduled
            };
            PersistentDataWrapper<DownloadFileJobList> persistentDataWrapper = await this._cache.Read<DownloadFileJobList>("downloadFileList.json", true, UpdateClientServiceConstants.DownloadDataFolder);
            if (persistentDataWrapper.Data == null)
                persistentDataWrapper.Data = new DownloadFileJobList();
            List<DownloadFileJob> jobList = persistentDataWrapper.Data.JobList;
            if ((jobList != null ? (jobList.Count<DownloadFileJob>((Func<DownloadFileJob, bool>)(j => j.DownloadFileJobId == job.DownloadFileJobId)) == 0 ? 1 : 0) : 0) != 0)
            {
                persistentDataWrapper.Data.JobList.Add(job);
                int num = await this._cache.Write<DownloadFileJobList>(persistentDataWrapper.Data, "downloadFileList.json", UpdateClientServiceConstants.DownloadDataFolder) ? 1 : 0;
            }
            DateTimeOffset startDateUtc = job.StartDateUtc;
            if (job.StartDateUtc == new DateTimeOffset() || job.StartDateUtc.Add(job.StartTime) < DateTimeOffset.Now)
                await this.ExecuteDownloadFileJob(job);
            IoTCommandResponse<ObjectResult> tcommandResponse = await this.SendDownloadFileJobExecutionStatusUpdate(execution);
        }

        private async Task<IoTCommandResponse<ObjectResult>> SendDownloadFileJobExecutionStatusUpdate(
          DownloadFileJobExecution execution)
        {
            return await this._iotCommandClient.PerformIoTCommand<ObjectResult>(new IoTCommandModel()
            {
                RequestId = Guid.NewGuid().ToString(),
                Version = 2,
                SourceId = this._store.KioskId.ToString(),
                Payload = (object)execution,
                Command = CommandEnum.DownloadFileJobExecutionStatusUpdate
            }, new PerformIoTCommandParameters()
            {
                RequestTimeout = new TimeSpan?(TimeSpan.FromSeconds(15.0)),
                IoTTopic = "$aws/rules/kioskrestcall",
                WaitForResponse = true
            });
        }

        public async Task ExecuteDownloadFileJob(DownloadFileJob job)
        {
            DownloadDataList downloads = await this._downloadService.GetDownloads(job.BitsJobId.ToString());
            if (downloads.Count == 0)
            {
                await this.AddDownloads(job);
            }
            else
            {
                if (!await this.HandleExistingDownloads(job, downloads))
                    return;
                await this.CleanupCache(job);
            }
        }

        public async Task CancelDownloadFileJob(DownloadFileJob job)
        {
            this._logger.LogInfoWithSource("Canceling job -> " + job.ToJson(), nameof(CancelDownloadFileJob), "/sln/src/UpdateClientService.API/Services/IoT/DownloadFiles/DownloadFilesService.cs");
            DownloadFileJobExecution execution = new DownloadFileJobExecution()
            {
                DownloadFileJobId = job.DownloadFileJobId,
                KioskId = this._store.KioskId,
                Status = JobStatus.CancelRequested
            };
            IoTCommandResponse<ObjectResult> tcommandResponse1 = await this.SendDownloadFileJobExecutionStatusUpdate(execution);
            foreach (DownloadData download in (List<DownloadData>)await this._downloadService.GetDownloads(job.BitsJobId.ToString()))
            {
                try
                {
                    DeleteDownloadResponse downloadResponse = await this._downloadService.CancelDownload(download.Key);
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "An error occurred while canceling " + download.Key, nameof(CancelDownloadFileJob), "/sln/src/UpdateClientService.API/Services/IoT/DownloadFiles/DownloadFilesService.cs");
                }
            }
            try
            {
                int num = await this._cache.DeleteLike(job.BitsJobId.ToString(), UpdateClientServiceConstants.DownloadDataFolder) ? 1 : 0;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("An error occurred while deleting jobs like {0}", (object)job.BitsJobId), nameof(CancelDownloadFileJob), "/sln/src/UpdateClientService.API/Services/IoT/DownloadFiles/DownloadFilesService.cs");
            }
            execution.Status = JobStatus.Canceled;
            IoTCommandResponse<ObjectResult> tcommandResponse2 = await this.SendDownloadFileJobExecutionStatusUpdate(execution);
        }

        public async Task<DownloadDataList> GetDownloadFileJobStatus(string bitsJobId)
        {
            return await this._downloadService.GetDownloads(bitsJobId);
        }

        private async Task CleanupCache(DownloadFileJob job)
        {
            PersistentDataWrapper<DownloadFileJobList> persistentDataWrapper = await this._cache.Read<DownloadFileJobList>("downloadFileList.json", filePath: UpdateClientServiceConstants.DownloadDataFolder);
            persistentDataWrapper.Data.JobList.RemoveAll((Predicate<DownloadFileJob>)(j => j.DownloadFileJobId == job.DownloadFileJobId));
            int num1 = await this._cache.Write<DownloadFileJobList>(persistentDataWrapper.Data, "downloadFileList.json", UpdateClientServiceConstants.DownloadDataFolder) ? 1 : 0;
            int num2 = await this._cache.DeleteLike(job.BitsJobId.ToString(), UpdateClientServiceConstants.DownloadDataFolder) ? 1 : 0;
        }

        private async Task AddDownloads(DownloadFileJob job)
        {
            DownloadFileJobExecution execution = new DownloadFileJobExecution()
            {
                DownloadFileJobId = job.DownloadFileJobId,
                KioskId = this._store.KioskId,
                EventTime = DateTime.Now,
                Status = JobStatus.InProgress
            };
            try
            {
                if (string.IsNullOrWhiteSpace(job.Metadata?.FileName) && string.IsNullOrWhiteSpace(job.Metadata?.ActivationScriptName))
                {
                    this._logger.LogErrorWithSource("FileName and ActivationScriptName cannot both be null", nameof(AddDownloads), "/sln/src/UpdateClientService.API/Services/IoT/DownloadFiles/DownloadFilesService.cs");
                    execution.CompletedOn = DateTime.Now;
                    execution.Status = JobStatus.Error;
                    await this.CleanupCache(job);
                    IoTCommandResponse<ObjectResult> tcommandResponse = await this.SendDownloadFileJobExecutionStatusUpdate(execution);
                    return;
                }
                this.SetProxiedUrls(job);
                if (!string.IsNullOrWhiteSpace(job.Metadata?.ActivationScriptName) && !string.IsNullOrWhiteSpace(job.PresignedScriptUrl))
                {
                    int num1 = await this._downloadService.AddDownload(this.DownloadKey("SCRIPT", job), (string)null, job.PresignedScriptUrl, DownloadPriority.Normal) ? 1 : 0;
                }
                if (!string.IsNullOrWhiteSpace(job.Metadata?.FileName) && !string.IsNullOrWhiteSpace(job.PresignedFileUrl))
                {
                    int num2 = await this._downloadService.AddDownload(this.DownloadKey("FILE", job), (string)null, job.PresignedFileUrl, DownloadPriority.Normal) ? 1 : 0;
                }
                IoTCommandResponse<ObjectResult> tcommandResponse1 = await this.SendDownloadFileJobExecutionStatusUpdate(execution);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "An error occurred while executing the download file job.", nameof(AddDownloads), "/sln/src/UpdateClientService.API/Services/IoT/DownloadFiles/DownloadFilesService.cs");
                execution.CompletedOn = DateTime.Now;
                execution.Status = JobStatus.Error;
                IoTCommandResponse<ObjectResult> tcommandResponse = await this.SendDownloadFileJobExecutionStatusUpdate(execution);
            }
        }

        private async Task<bool> HandleExistingDownloads(
          DownloadFileJob job,
          DownloadDataList downloads)
        {
            DownloadFileJobExecution execution = new DownloadFileJobExecution()
            {
                DownloadFileJobId = job.DownloadFileJobId,
                KioskId = this._store.KioskId,
                EventTime = DateTime.Now,
                Status = JobStatus.InProgress
            };
            IoTCommandModel ioTcommandModel = new IoTCommandModel()
            {
                Command = CommandEnum.DownloadFileJobExecutionStatusUpdate,
                Payload = (object)new MqttResponse<DownloadFileJobExecution>()
                {
                    Data = execution
                }
            };
            try
            {
                if (downloads.All<DownloadData>((Func<DownloadData, bool>)(d => d.DownloadState == DownloadState.Downloading)))
                    return false;
                if (downloads.Any<DownloadData>((Func<DownloadData, bool>)(d => d.DownloadState == DownloadState.Error)))
                    execution.Status = JobStatus.Error;
                if (downloads.All<DownloadData>((Func<DownloadData, bool>)(d => d.DownloadState == DownloadState.PostDownload)))
                {
                    foreach (DownloadData download in (List<DownloadData>)downloads)
                    {
                        if (execution.Status != JobStatus.Error)
                        {
                            if (download.Key == this.DownloadKey("FILE", job) && !string.IsNullOrWhiteSpace(job.Metadata?.FileName))
                                this.SaveFileToDestination(job, download);
                            if (download.Key == this.DownloadKey("SCRIPT", job) && !string.IsNullOrWhiteSpace(job.Metadata?.ActivationScriptName))
                            {
                                DownloadFileMetadata metadata = job.Metadata;
                                int num;
                                if (metadata == null)
                                {
                                    num = 0;
                                }
                                else
                                {
                                    string activationScriptName = metadata.ActivationScriptName;
                                    bool? nullable = activationScriptName != null ? new bool?(!activationScriptName.EndsWith(".ps1")) : new bool?();
                                    bool flag = true;
                                    num = nullable.GetValueOrDefault() == flag & nullable.HasValue ? 1 : 0;
                                }
                                if (num != 0)
                                {
                                    this._logger.LogErrorWithSource("Script " + (job.Metadata.ActivationScriptName ?? "null") + " does not have a valid PowerShell extension of '.ps1'", nameof(HandleExistingDownloads), "/sln/src/UpdateClientService.API/Services/IoT/DownloadFiles/DownloadFilesService.cs");
                                    execution.Status = JobStatus.Error;
                                }
                                else
                                    execution.Status = this.TryDownloadAndExecuteScript(job, download) ? execution.Status : JobStatus.Error;
                            }
                        }
                        else
                            break;
                    }
                    if (execution.Status != JobStatus.Error)
                        execution.Status = JobStatus.Complete;
                    execution.CompletedOn = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "An error occurred while executing the download file job.", nameof(HandleExistingDownloads), "/sln/src/UpdateClientService.API/Services/IoT/DownloadFiles/DownloadFilesService.cs");
                execution.CompletedOn = DateTime.Now;
                execution.Status = JobStatus.Error;
            }
            bool isComplete = execution.Status == JobStatus.Complete || execution.Status == JobStatus.Error;
            if (isComplete)
            {
                IoTCommandResponse<ObjectResult> tcommandResponse = await this.SendDownloadFileJobExecutionStatusUpdate(execution);
            }
            return isComplete;
        }

        public async Task HandleScheduledJobs()
        {
            try
            {
                PersistentDataWrapper<DownloadFileJobList> persistentDataWrapper = await this._cache.Read<DownloadFileJobList>("downloadFileList.json", filePath: UpdateClientServiceConstants.DownloadDataFolder);
                if (persistentDataWrapper.Data == null)
                    persistentDataWrapper.Data = new DownloadFileJobList();
                DownloadFileJobList data = persistentDataWrapper.Data;
                List<DownloadFileJob> jobList1 = persistentDataWrapper.Data.JobList;
                List<DownloadFileJob> list = jobList1 != null ? jobList1.Distinct<DownloadFileJob>((IEqualityComparer<DownloadFileJob>)new DownloadFileJobComparer()).ToList<DownloadFileJob>() : (List<DownloadFileJob>)null;
                data.JobList = list;
                ILogger<DownloadFilesService> logger = this._logger;
                List<DownloadFileJob> jobList2 = persistentDataWrapper.Data.JobList;
                string str = "Handling scheduled jobs, " + ((jobList2 != null ? jobList2.ToJson() : (string)null) ?? "null");
                this._logger.LogInfoWithSource(str, nameof(HandleScheduledJobs), "/sln/src/UpdateClientService.API/Services/IoT/DownloadFiles/DownloadFilesService.cs");
                foreach (DownloadFileJob job in persistentDataWrapper.Data.JobList)
                {
                    if (job.StartDateUtc.Add(job.StartTime) < DateTimeOffset.Now)
                        await this.ExecuteDownloadFileJob(job);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while handling schedule jobs", nameof(HandleScheduledJobs), "/sln/src/UpdateClientService.API/Services/IoT/DownloadFiles/DownloadFilesService.cs");
            }
        }

        private void SetProxiedUrls(DownloadFileJob job)
        {
            if (!string.IsNullOrWhiteSpace(job.Metadata?.FileName))
                job.PresignedFileUrl = new Uri(string.Format("{0}/{1}/{2}/{3}", (object)this._settings.BaseServiceUrl, (object)"api/downloads/s3/proxy", (object)job.FileKey, (object)DownloadPathType.DownloadFile)).ToString();
            if (string.IsNullOrWhiteSpace(job.Metadata?.ActivationScriptName))
                return;
            job.PresignedScriptUrl = new Uri(string.Format("{0}/{1}/{2}/{3}", (object)this._settings.BaseServiceUrl, (object)"api/downloads/s3/proxy", (object)job.FileKey, (object)DownloadPathType.ActivationScript)).ToString();
        }

        private void SaveFileToDestination(DownloadFileJob job, DownloadData download)
        {
            string destFileName = Path.Combine(job.Metadata.DestinationPath, job.Metadata.FileName);
            Directory.CreateDirectory(job.Metadata.DestinationPath);
            File.Copy(download.Path, destFileName);
        }

        private bool TryDownloadAndExecuteScript(DownloadFileJob job, DownloadData download)
        {
            string str = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ps1");
            Directory.CreateDirectory(Path.GetTempPath());
            File.Copy(download.Path, str);
            bool flag = !this._cmdService.TryExecutePowerShellScriptFromFile(str);
            File.Delete(str);
            return flag;
        }

        private string DownloadKey(string prefix, DownloadFileJob job)
        {
            return string.Format("{0}.{1}", (object)prefix, (object)job.BitsJobId);
        }
    }
}
