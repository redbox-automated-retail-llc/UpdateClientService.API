using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Configuration;
using UpdateClientService.API.Services.DownloadService;
using UpdateClientService.API.Services.FileCache;
using UpdateClientService.API.Services.IoT.FileSets;
using UpdateClientService.API.Services.Kernel;
using UpdateClientService.API.Services.KioskEngine;

namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetService : IFileSetService
    {
        private static readonly string _root = Constants.FileSetsRoot;
        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private static readonly TimeSpan _processLockTimeout = TimeSpan.FromSeconds(20.0);
        private static DateTime _lastCallToProcessPendingFileSetVersions = DateTime.MinValue;
        private static SemaphoreSlim _lockPendingFileSetVersions = new SemaphoreSlim(1, 1);
        private readonly int _pendingFileSetVersionsLockTimeout = 5000;
        private readonly IFileCacheService _fileCacheService;
        private readonly IDownloadService _downloadService;
        private readonly IDownloader _downloader;
        private readonly IChangeSetFileService _changeSetFileService;
        private readonly ILogger<FileSetService> _logger;
        private readonly IKioskFileSetVersionsService _versionsService;
        private readonly IKernelService _kernelService;
        private readonly IKioskEngineService _kioskEngineService;
        private readonly IStateFileService _stateFileService;
        private readonly IFileSetTransition _fileSetTransition;
        private readonly IOptionsMonitorKioskConfiguration _optionsMonitorKioskConfiguration;
        private readonly FileSetSettings _settings;

        public FileSetService(
          IFileCacheService fileCacheService,
          IDownloadService downloadService,
          IDownloader downloader,
          IChangeSetFileService changeSetFileService,
          ILogger<FileSetService> logger,
          IKioskFileSetVersionsService versionsService,
          IKernelService kernelService,
          IKioskEngineService kioskEngineService,
          IStateFileService stateFileService,
          IOptionsMonitor<AppSettings> settings,
          IFileSetTransition fileSetTransition,
          IOptionsMonitorKioskConfiguration optionsKioskConfiguration)
        {
            this._fileCacheService = fileCacheService;
            this._downloadService = downloadService;
            this._downloader = downloader;
            this._changeSetFileService = changeSetFileService;
            this._logger = logger;
            this._versionsService = versionsService;
            this._kernelService = kernelService;
            this._kioskEngineService = kioskEngineService;
            this._stateFileService = stateFileService;
            this._settings = settings.CurrentValue.FileSet;
            this._fileSetTransition = fileSetTransition;
            this._optionsMonitorKioskConfiguration = optionsKioskConfiguration;
            this.Initialize();
        }

        public bool Initialize()
        {
            try
            {
                Directory.CreateDirectory(FileSetService._root);
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while initializing FileSetService", nameof(Initialize), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                return false;
            }
        }

        public async Task<ProcessChangeSetResponse> ProcessChangeSet(
          ClientFileSetRevisionChangeSet clientFileSetRevisionChangeSet)
        {
            FileSetService fileSetService = this;
            try
            {
                if (clientFileSetRevisionChangeSet == null)
                {
                    this._logger.LogErrorWithSource("Changeset is null.", nameof(ProcessChangeSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                    ProcessChangeSetResponse changeSetResponse = new ProcessChangeSetResponse();
                    changeSetResponse.StatusCode = HttpStatusCode.InternalServerError;
                    return changeSetResponse;
                }
                ILogger<FileSetService> logger1 = fileSetService._logger;
                ClientFileSetRevisionChangeSet revisionChangeSetKey1 = clientFileSetRevisionChangeSet;
                string str1 = "Starting processing for " + (revisionChangeSetKey1 != null ? revisionChangeSetKey1.IdentifyingText() : (string)null);
                this._logger.LogInfoWithSource(str1, nameof(ProcessChangeSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                if (!fileSetService._optionsMonitorKioskConfiguration.FileSets.FileSetDownloadEnabled)
                {
                    ILogger<FileSetService> logger2 = fileSetService._logger;
                    ClientFileSetRevisionChangeSet revisionChangeSetKey2 = clientFileSetRevisionChangeSet;
                    string str2 = "FileSet downloads are disabled by config setting FileSets.FileSetDownloadEnabled.  Aborting FileSet download for " + (revisionChangeSetKey2 != null ? revisionChangeSetKey2.IdentifyingText() : (string)null) + " ";
                    this._logger.LogWarningWithSource(str2, nameof(ProcessChangeSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                    ProcessChangeSetResponse changeSetResponse = new ProcessChangeSetResponse();
                    changeSetResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                    return changeSetResponse;
                }
                try
                {
                    if (!await FileSetService._lock.WaitAsync(FileSetService._processLockTimeout))
                    {
                        ILogger<FileSetService> logger3 = fileSetService._logger;
                        ClientFileSetRevisionChangeSet revisionChangeSetKey3 = clientFileSetRevisionChangeSet;
                        string str3 = "Unable to aquire lock for processing " + (revisionChangeSetKey3 != null ? revisionChangeSetKey3.IdentifyingText() : (string)null) + ".";
                        this._logger.LogWarningWithSource(str3, nameof(ProcessChangeSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                        ProcessChangeSetResponse changeSetResponse = new ProcessChangeSetResponse();
                        changeSetResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
                        return changeSetResponse;
                    }
                }
                catch (Exception ex)
                {
                    ILogger<FileSetService> logger4 = fileSetService._logger;
                    Exception exception = ex;
                    ClientFileSetRevisionChangeSet revisionChangeSetKey4 = clientFileSetRevisionChangeSet;
                    string str4 = "Exception while trying to aquire lock for processing " + (revisionChangeSetKey4 != null ? revisionChangeSetKey4.IdentifyingText() : (string)null) + ".";
                    this._logger.LogErrorWithSource(exception, str4, nameof(ProcessChangeSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                    ProcessChangeSetResponse changeSetResponse = new ProcessChangeSetResponse();
                    changeSetResponse.StatusCode = HttpStatusCode.InternalServerError;
                    return changeSetResponse;
                }
                try
                {
                    bool flag = await fileSetService.StartRevisionChangeSetProcessing((IEnumerable<ClientFileSetRevisionChangeSet>)new List<ClientFileSetRevisionChangeSet>()
          {
            clientFileSetRevisionChangeSet
          });
                    FileSetPollRequest fileSetPollRequest;
                    if (fileSetService.CreateFileSetPollRequest((RevisionChangeSetKey)clientFileSetRevisionChangeSet, !flag ? FileSetState.Error : FileSetState.InProgress, out fileSetPollRequest))
                    {
                        ReportFileSetVersionsResponse versionsResponse = await fileSetService._versionsService.ReportFileSetVersion(fileSetPollRequest);
                    }
                }
                finally
                {
                    SemaphoreSlim semaphoreSlim = FileSetService._lock;
                    if ((semaphoreSlim != null ? (semaphoreSlim.CurrentCount == 0 ? 1 : 0) : 0) != 0)
                        FileSetService._lock.Release();
                }
            }
            catch (Exception ex)
            {
                ILogger<FileSetService> logger = fileSetService._logger;
                if (logger != null)
                {
                    Exception exception = ex;
                    ClientFileSetRevisionChangeSet revisionChangeSetKey = clientFileSetRevisionChangeSet;
                    string str = "Exception while processing changeset " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                    this._logger.LogErrorWithSource(exception, str, nameof(ProcessChangeSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                }
                ProcessChangeSetResponse changeSetResponse = new ProcessChangeSetResponse();
                changeSetResponse.StatusCode = HttpStatusCode.InternalServerError;
                return changeSetResponse;
            }
            ProcessChangeSetResponse changeSetResponse1 = new ProcessChangeSetResponse();
            changeSetResponse1.StatusCode = HttpStatusCode.OK;
            return changeSetResponse1;
        }

        private async Task<bool> StartRevisionChangeSetProcessing(
          IEnumerable<ClientFileSetRevisionChangeSet> clientFileSetRevisionChangeSets)
        {
            bool result = true;
            try
            {
                foreach (ClientFileSetRevisionChangeSet eachClientFileSetRevisionChangeSet in clientFileSetRevisionChangeSets)
                {
                    if (eachClientFileSetRevisionChangeSet.Action == FileSetAction.Delete)
                    {
                        ILogger<FileSetService> logger = this._logger;
                        ClientFileSetRevisionChangeSet revisionChangeSetKey = eachClientFileSetRevisionChangeSet;
                        string str = "Deleting " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                        this._logger.LogInfoWithSource(str, nameof(StartRevisionChangeSetProcessing), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                        bool flag = result;
                        if (flag)
                            flag = await this._changeSetFileService.Delete(RevisionChangeSet.Create(eachClientFileSetRevisionChangeSet));
                        result = flag;
                        if (!await this._stateFileService.Delete(eachClientFileSetRevisionChangeSet.FileSetId))
                        {
                            result = false;
                            this._logger.LogErrorWithSource(string.Format("Unable to delete StateFile for FileSetId {0}", (object)eachClientFileSetRevisionChangeSet.FileSetId), nameof(StartRevisionChangeSetProcessing), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                        }
                    }
                    else if (eachClientFileSetRevisionChangeSet.Action == FileSetAction.Update)
                    {
                        bool flag = result;
                        if (flag)
                            flag = await this.StartRevisionChangeSetProcessing(eachClientFileSetRevisionChangeSet);
                        result = flag;
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
                this._logger.LogErrorWithSource(ex, "Exception while starting RevisionChangeSet processing", nameof(StartRevisionChangeSetProcessing), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
            }
            return result;
        }

        private async Task<bool> StartRevisionChangeSetProcessing(
          ClientFileSetRevisionChangeSet clientFileSetRevisionChangeSet)
        {
            bool result = false;
            try
            {
                ILogger<FileSetService> logger = this._logger;
                ClientFileSetRevisionChangeSet revisionChangeSetKey = clientFileSetRevisionChangeSet;
                string str = "Starting processing for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogInfoWithSource(str, nameof(StartRevisionChangeSetProcessing), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                if (await this._changeSetFileService.CreateRevisionChangeSet(clientFileSetRevisionChangeSet))
                {
                    if (await this.CreateStateFileFromChangeSet(clientFileSetRevisionChangeSet))
                        result = true;
                    else
                        this._logger.LogErrorWithSource(string.Format("Unable to create StateFile for FileSetId {0}", (object)clientFileSetRevisionChangeSet?.FileSetId), nameof(StartRevisionChangeSetProcessing), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while starting processing of ClientFileSetRevisionChangeSet", nameof(StartRevisionChangeSetProcessing), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
            }
            return result;
        }

        private async Task<bool> CreateStateFileFromChangeSet(
          ClientFileSetRevisionChangeSet clientFileSetRevisionChangeSet)
        {
            StateFile stateFile = (await this._stateFileService.Get(clientFileSetRevisionChangeSet.FileSetId))?.StateFile ?? new StateFile(clientFileSetRevisionChangeSet.FileSetId, 0L, clientFileSetRevisionChangeSet.RevisionId, FileSetState.InProgress);
            stateFile.InProgressRevisionId = clientFileSetRevisionChangeSet.RevisionId;
            stateFile.InProgressFileSetState = FileSetState.InProgress;
            StateFileResponse stateFileResponse = await this._stateFileService.Save(stateFile);
            return stateFileResponse != null && stateFileResponse.StatusCode == HttpStatusCode.OK;
        }

        public async Task ProcessInProgressRevisionChangeSets()
        {
            try
            {
                TaskAwaiter<bool> taskAwaiter = FileSetService._lock.WaitAsync(FileSetService._processLockTimeout).GetAwaiter();
                if (!taskAwaiter.IsCompleted)
                {
                    TaskAwaiter<bool> taskAwaiter2;
                    taskAwaiter = taskAwaiter2;
                    taskAwaiter2 = default(TaskAwaiter<bool>);
                }
                if (!taskAwaiter.GetResult())
                {
                    this._logger.LogWarningWithSource("Unable to aquire lock.", "ProcessInProgressRevisionChangeSets", "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                }
                else
                {
                    FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData = new FileSetService.RevisionChangeSetsProcessingData();
                    try
                    {
                        await this.GetInProgressStateFiles(revisionChangeSetsProcessingData);
                        await this.GetRevisionChangeSets(revisionChangeSetsProcessingData);
                        await this.DeleteBadRevisionChangeSets(revisionChangeSetsProcessingData);
                        await this.ProcessChangeSets(revisionChangeSetsProcessingData);
                        await this.ProcessNeedsDependencyStates(revisionChangeSetsProcessingData);
                        await this.ProcessActivationPendingStates(revisionChangeSetsProcessingData);
                        await this.ProcessBeforeActivationActions(revisionChangeSetsProcessingData);
                        await this.ProcessActivating(revisionChangeSetsProcessingData);
                        await this.ProcessAfterActivationActions(revisionChangeSetsProcessingData);
                        await this.ProcessActivatedStates(revisionChangeSetsProcessingData);
                        await this.ProcessErrorStates(revisionChangeSetsProcessingData);
                        await this.RebootIfNeeded(revisionChangeSetsProcessingData);
                        this.ProcessPostDownloadStates(revisionChangeSetsProcessingData);
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogErrorWithSource(ex, "Exception while processing in progress RevisionChangeSets", "ProcessInProgressRevisionChangeSets", "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                    }
                    finally
                    {
                        FileSetService._lock.Release();
                    }
                    revisionChangeSetsProcessingData = null;
                }
            }
            catch (Exception ex2)
            {
                this._logger.LogErrorWithSource(ex2, "Exception while processing in progress RevisionChangeSets.", "ProcessInProgressRevisionChangeSets", "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
            }
        }

        private void ProcessPostDownloadStates(
      FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            foreach (DownloadData downloadData in (List<DownloadData>)revisionChangeSetsProcessingData.AllDownloadData)
            {
                if (downloadData.DownloadState == DownloadState.PostDownload)
                {
                    this._logger.LogInfoWithSource("Completing leftover DownloadData " + downloadData.FileName, nameof(ProcessPostDownloadStates), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                    this._downloader.Complete(downloadData);
                }
            }
        }

        private async Task RebootIfNeeded(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete || !this._fileSetTransition.RebootRequired)
                return;
            int num = await this.Reboot() ? 1 : 0;
        }

        private async Task ProcessErrorStates(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            foreach (RevisionChangeSet eachRevisionChangeSet in revisionChangeSetsProcessingData.RevisionChangeSets.Where<RevisionChangeSet>((Func<RevisionChangeSet, bool>)(x => x.State == ChangesetState.Error)))
            {
                StateFile stateFile = revisionChangeSetsProcessingData.GetStateFileForRevisionChangeSet(eachRevisionChangeSet);
                if (stateFile != null)
                {
                    if (stateFile.InProgressFileSetState != FileSetState.NeedsDependency)
                    {
                        stateFile.InProgressFileSetState = FileSetState.Error;
                        StateFileResponse stateFileResponse = await this._stateFileService.Save(stateFile);
                    }
                    FileSetPollRequest fileSetPollRequest;
                    bool flag = this.CreateFileSetPollRequest((RevisionChangeSetKey)eachRevisionChangeSet, stateFile.InProgressFileSetState, out fileSetPollRequest);
                    if (flag)
                        flag = (await this._versionsService.ReportFileSetVersion(fileSetPollRequest)).StatusCode == HttpStatusCode.OK;
                    if (flag)
                    {
                        int num1 = await this._changeSetFileService.Delete(eachRevisionChangeSet) ? 1 : 0;
                        int num2 = await this._stateFileService.DeleteInProgress(stateFile.FileSetId) ? 1 : 0;
                    }
                }
                stateFile = (StateFile)null;
            }
        }

        private async Task ProcessActivatedStates(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            foreach (RevisionChangeSet eachRevisionChangeSet in revisionChangeSetsProcessingData.RevisionChangeSets.Where<RevisionChangeSet>((Func<RevisionChangeSet, bool>)(x => x.State == ChangesetState.Activated)))
            {
                StateFile revisionChangeSet = revisionChangeSetsProcessingData.GetStateFileForRevisionChangeSet(eachRevisionChangeSet);
                if (revisionChangeSet != null)
                {
                    revisionChangeSet.InProgressFileSetState = FileSetState.Active;
                    StateFileResponse stateFileResponse = await this._stateFileService.Save(revisionChangeSet);
                }
                int num = await this._changeSetFileService.Delete(eachRevisionChangeSet) ? 1 : 0;
                FileSetPollRequest fileSetPollRequest;
                if (this.CreateFileSetPollRequest((RevisionChangeSetKey)eachRevisionChangeSet, FileSetState.Active, out fileSetPollRequest))
                {
                    ReportFileSetVersionsResponse versionsResponse = await this._versionsService.ReportFileSetVersion(fileSetPollRequest);
                }
            }
        }

        private async Task ProcessAfterActivationActions(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            foreach (RevisionChangeSet revisionChangeSet in revisionChangeSetsProcessingData.RevisionChangeSets)
                await this._changeSetFileService.ProcessActivationAfterActions(revisionChangeSet);
        }

        private async Task ProcessActivating(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            foreach (RevisionChangeSet revisionChangeSet in revisionChangeSetsProcessingData.RevisionChangeSets)
                await this._changeSetFileService.ProcessActivating(revisionChangeSet);
        }

        private async Task ProcessBeforeActivationActions(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            foreach (RevisionChangeSet revisionChangeSet in revisionChangeSetsProcessingData.RevisionChangeSets)
                await this._changeSetFileService.ProcessActivationBeforeActions(revisionChangeSet);
        }

        private async Task ProcessActivationPendingStates(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            foreach (RevisionChangeSet revisionChangeSet in revisionChangeSetsProcessingData.RevisionChangeSets)
                await this._changeSetFileService.ProcessActivationPending(revisionChangeSet);
        }

        private async Task ProcessNeedsDependencyStates(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            Dictionary<long, FileSetDependencyState> dependencyStates = await this.GetDependencyStates();
            if (dependencyStates != null)
            {
                foreach (RevisionChangeSet revisionChangeSet in revisionChangeSetsProcessingData.RevisionChangeSets)
                    await this._changeSetFileService.ProcessActivationDependencyCheck(dependencyStates, revisionChangeSet);
            }
            foreach (RevisionChangeSet revisionChangeSet1 in revisionChangeSetsProcessingData.RevisionChangeSets)
            {
                if (revisionChangeSet1.State == ChangesetState.Error && revisionChangeSet1.Message == FileSetState.NeedsDependency.ToString())
                {
                    StateFile revisionChangeSet2 = revisionChangeSetsProcessingData.GetStateFileForRevisionChangeSet(revisionChangeSet1);
                    if (revisionChangeSet2 != null)
                    {
                        revisionChangeSet2.InProgressFileSetState = FileSetState.NeedsDependency;
                        StateFileResponse stateFileResponse = await this._stateFileService.Save(revisionChangeSet2);
                    }
                }
            }
            dependencyStates = (Dictionary<long, FileSetDependencyState>)null;
        }

        private async Task ProcessChangeSets(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            FileSetService.RevisionChangeSetsProcessingData setsProcessingData = revisionChangeSetsProcessingData;
            setsProcessingData.AllDownloadData = await this._downloadService.GetDownloads(new Regex("^(?!(FILE|SCRIPT|downloadFileList.json)).*"));
            setsProcessingData = (FileSetService.RevisionChangeSetsProcessingData)null;
            if (revisionChangeSetsProcessingData.RevisionChangeSets.Count<RevisionChangeSet>() == 0 && revisionChangeSetsProcessingData.AllDownloadData.Count<DownloadData>() == 0)
            {
                revisionChangeSetsProcessingData.IsComplete = true;
            }
            else
            {
                foreach (RevisionChangeSet revisionChangeSet in revisionChangeSetsProcessingData.RevisionChangeSets)
                    await this._changeSetFileService.ProcessChangeSet(revisionChangeSetsProcessingData.AllDownloadData, revisionChangeSet);
            }
        }

        private async Task DeleteBadRevisionChangeSets(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            List<RevisionChangeSet> list = revisionChangeSetsProcessingData.RevisionChangeSets.Where<RevisionChangeSet>((Func<RevisionChangeSet, bool>)(revisionChangeSet => !revisionChangeSetsProcessingData.InProgressStateFiles.Any<StateFile>((Func<StateFile, bool>)(stateFile => stateFile.FileSetId == revisionChangeSet.FileSetId && stateFile.InProgressRevisionId == revisionChangeSet.RevisionId)))).ToList<RevisionChangeSet>();
            List<StateFile> badStateFiles = revisionChangeSetsProcessingData.InProgressStateFiles.Where<StateFile>((Func<StateFile, bool>)(stateFile => !revisionChangeSetsProcessingData.RevisionChangeSets.Any<RevisionChangeSet>((Func<RevisionChangeSet, bool>)(revisionChangeSet => stateFile.FileSetId == revisionChangeSet.FileSetId && stateFile.InProgressRevisionId == revisionChangeSet.RevisionId)))).ToList<StateFile>();
            foreach (RevisionChangeSet changeset in list)
            {
                revisionChangeSetsProcessingData.RevisionChangeSets.Remove(changeset);
                int num = await this._changeSetFileService.Delete(changeset) ? 1 : 0;
            }
            foreach (StateFile stateFile in badStateFiles)
            {
                int num = await this._stateFileService.DeleteInProgress(stateFile.FileSetId) ? 1 : 0;
            }
            badStateFiles = (List<StateFile>)null;
        }

        private async Task GetRevisionChangeSets(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            if (revisionChangeSetsProcessingData.IsComplete)
                return;
            FileSetService.RevisionChangeSetsProcessingData setsProcessingData = revisionChangeSetsProcessingData;
            setsProcessingData.RevisionChangeSets = await this._changeSetFileService.GetAllRevisionChangeSets();
            setsProcessingData = (FileSetService.RevisionChangeSetsProcessingData)null;
        }

        private async Task GetInProgressStateFiles(
          FileSetService.RevisionChangeSetsProcessingData revisionChangeSetsProcessingData)
        {
            revisionChangeSetsProcessingData.InProgressStateFiles = (await this._stateFileService.GetAllInProgress())?.StateFiles;
            if (revisionChangeSetsProcessingData.InProgressStateFiles == null)
                revisionChangeSetsProcessingData.IsComplete = true;
            if (!revisionChangeSetsProcessingData.InProgressStateFiles.Any<StateFile>())
                return;
            this._logger.LogInfoWithSource(string.Format("Processing {0} in progress RevisionChangeSet", (object)revisionChangeSetsProcessingData.InProgressStateFiles.Count), nameof(GetInProgressStateFiles), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
        }

        private bool CreateFileSetPollRequest(
          RevisionChangeSetKey revisionChangeSetKey,
          FileSetState state,
          out FileSetPollRequest fileSetPollRequest)
        {
            fileSetPollRequest = (FileSetPollRequest)null;
            try
            {
                if (revisionChangeSetKey != null)
                {
                    if (revisionChangeSetKey.FileSetId > 0L)
                    {
                        if (revisionChangeSetKey.RevisionId > 0L)
                            fileSetPollRequest = new FileSetPollRequest()
                            {
                                FileSetId = revisionChangeSetKey.FileSetId,
                                FileSetRevisionId = revisionChangeSetKey.RevisionId,
                                FileSetState = state
                            };
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "An unhandled exception occurred.", nameof(CreateFileSetPollRequest), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
            }
            return fileSetPollRequest != null;
        }

        private async Task<Dictionary<long, FileSetDependencyState>> GetDependencyStates()
        {
            Dictionary<long, FileSetDependencyState> dependencies = new Dictionary<long, FileSetDependencyState>();
            try
            {
                List<StateFile> stateFiles = (await this._stateFileService.GetAll())?.StateFiles;
                if (stateFiles == null)
                    return (Dictionary<long, FileSetDependencyState>)null;
                List<RevisionChangeSet> revisionChangeSets = await this._changeSetFileService.GetAllRevisionChangeSets();
                foreach (StateFile stateFile in stateFiles)
                {
                    StateFile eachStateFile = stateFile;
                    FileSetDependencyState fileSetDependencyState = new FileSetDependencyState()
                    {
                        FileSetId = eachStateFile.FileSetId,
                        IsInProgressStaged = false
                    };
                    if (eachStateFile.IsRevisionDownloadInProgress)
                    {
                        RevisionChangeSet revisionChangeSet = revisionChangeSets.Where<RevisionChangeSet>((Func<RevisionChangeSet, bool>)(cs => cs.FileSetId == eachStateFile.FileSetId)).FirstOrDefault<RevisionChangeSet>();
                        if (revisionChangeSet != null)
                        {
                            ClientFileSetRevision clientFileSetRevision = await this._fileCacheService.GetClientFileSetRevision((RevisionChangeSetKey)revisionChangeSet);
                            if (clientFileSetRevision != null)
                            {
                                fileSetDependencyState.InProgressRevisionId = revisionChangeSet.RevisionId;
                                fileSetDependencyState.InProgressVersion = clientFileSetRevision.RevisionVersion;
                                fileSetDependencyState.IsInProgressStaged = revisionChangeSet.IsStaged;
                            }
                        }
                        revisionChangeSet = (RevisionChangeSet)null;
                    }
                    if (eachStateFile.HasActiveRevision)
                    {
                        fileSetDependencyState.ActiveRevisionId = eachStateFile.ActiveRevisionId;
                        ClientFileSetRevision clientFileSetRevision = await this._fileCacheService.GetAnyClientFileSetRevision(eachStateFile.FileSetId, eachStateFile.ActiveRevisionId);
                        if (clientFileSetRevision != null)
                            fileSetDependencyState.ActiveVersion = clientFileSetRevision.RevisionVersion;
                    }
                    dependencies[eachStateFile.FileSetId] = fileSetDependencyState;
                    fileSetDependencyState = (FileSetDependencyState)null;
                }
                return dependencies;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while getting FileSetDependencyStates", nameof(GetDependencyStates), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                return (Dictionary<long, FileSetDependencyState>)null;
            }
        }

        private async Task<bool> Reboot()
        {
            this._fileSetTransition.ClearRebootRequired();
            bool result = false;
            PerformShutdownResponse shutdownResponse = await this._kioskEngineService.PerformShutdown(this._settings.KioskEngineShutdownTimeoutMs, this._settings.KioskEngineShutdownTimeoutMs);
            if (!string.IsNullOrWhiteSpace(shutdownResponse?.Error))
                this._logger.LogErrorWithSource("Unable to shutdown KioskEngine.  Error Message: " + shutdownResponse?.Error, nameof(Reboot), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
            else if (!this._kernelService.PerformShutdown(ShutdownType.Reboot))
                this._logger.LogErrorWithSource("Error while trying to perform reboot.", nameof(Reboot), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
            else
                result = true;
            return result;
        }

        public async Task<ReportFileSetVersionsResponse> ProcessPendingFileSetVersions()
        {
            try
            {
                await this.SetLastCallToProcessPendingFileSetVersions();
                ReportFileSetVersionsResponse result = await this._versionsService.ReportFileSetVersions();
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    List<ClientFileSetRevisionChangeSet> revisionChangeSets = result.ClientFileSetRevisionChangeSets;
                    if ((revisionChangeSets != null ? (revisionChangeSets.Any<ClientFileSetRevisionChangeSet>() ? 1 : 0) : 0) != 0)
                    {
                        foreach (ClientFileSetRevisionChangeSet revisionChangeSet in result.ClientFileSetRevisionChangeSets)
                        {
                            ProcessChangeSetResponse changeSetResponse = await this.ProcessChangeSet(revisionChangeSet);
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while processing pending FileSet versions", nameof(ProcessPendingFileSetVersions), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                ReportFileSetVersionsResponse versionsResponse = new ReportFileSetVersionsResponse();
                versionsResponse.StatusCode = HttpStatusCode.InternalServerError;
                return versionsResponse;
            }
        }

        private async Task<DateTime> GetLastCallToProcessPendingFileSetVersion()
        {
            DateTime result = DateTime.MinValue;
            if (await FileSetService._lockPendingFileSetVersions.WaitAsync(this._pendingFileSetVersionsLockTimeout))
            {
                try
                {
                    result = FileSetService._lastCallToProcessPendingFileSetVersions;
                }
                finally
                {
                    FileSetService._lockPendingFileSetVersions.Release();
                }
            }
            else
                this._logger.LogErrorWithSource("Lock failed. Unable to get last call date for ProcessPendingFileSetVersion", nameof(GetLastCallToProcessPendingFileSetVersion), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
            return result;
        }

        private async Task SetLastCallToProcessPendingFileSetVersions()
        {
            if (await FileSetService._lockPendingFileSetVersions.WaitAsync(this._pendingFileSetVersionsLockTimeout))
            {
                try
                {
                    FileSetService._lastCallToProcessPendingFileSetVersions = DateTime.Now;
                }
                finally
                {
                    FileSetService._lockPendingFileSetVersions.Release();
                }
            }
            else
                this._logger.LogErrorWithSource("Lock failed. Unable to set last call for ProcessPendingFileSetVersion", nameof(SetLastCallToProcessPendingFileSetVersions), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
        }

        public async Task<ReportFileSetVersionsResponse> TriggerProcessPendingFileSetVersions(
          TriggerReportFileSetVersionsRequest triggerReportFileSetVersionsRequest)
        {
            ReportFileSetVersionsResponse response = new ReportFileSetVersionsResponse();
            try
            {
                if (triggerReportFileSetVersionsRequest != null)
                {
                    if (triggerReportFileSetVersionsRequest.ExecutionTimeFrameMs.HasValue)
                    {
                        long? executionTimeFrameMs = triggerReportFileSetVersionsRequest.ExecutionTimeFrameMs;
                        long num = 0;
                        if (!(executionTimeFrameMs.GetValueOrDefault() == num & executionTimeFrameMs.HasValue))
                        {
                            await Task.Run((async () =>
                            {
                                int randomMs = new Random().Next((int)triggerReportFileSetVersionsRequest.ExecutionTimeFrameMs.Value);
                                this._logger.LogInfoWithSource(string.Format("waiting {0} ms before calling ReportFileSetVersions", (object)randomMs), nameof(TriggerProcessPendingFileSetVersions), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                                DateTime lastCall = await this.GetLastCallToProcessPendingFileSetVersion();
                                await Task.Delay(randomMs);
                                DateTime dateTime = lastCall;
                                if (dateTime != await this.GetLastCallToProcessPendingFileSetVersion())
                                    return;
                                ReportFileSetVersionsResponse versionsResponse = await this.ProcessPendingFileSetVersions();
                            }));
                            return response;
                        }
                    }
                    response = await this.ProcessPendingFileSetVersions();
                }
                else
                {
                    this._logger.LogErrorWithSource("parameter TriggerReportFileSetVersionsRequest must not be null.", nameof(TriggerProcessPendingFileSetVersions), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                    response.StatusCode = HttpStatusCode.InternalServerError;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception triggering ProcessPendingFileSetVersions.", nameof(TriggerProcessPendingFileSetVersions), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetService.cs");
                response.StatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        private class RevisionChangeSetsProcessingData
        {
            public bool IsComplete { get; set; }

            public List<StateFile> InProgressStateFiles { get; set; }

            public List<RevisionChangeSet> RevisionChangeSets { get; set; }

            public DownloadDataList AllDownloadData { get; set; }

            public StateFile GetStateFileForRevisionChangeSet(RevisionChangeSet revisionChangeSet)
            {
                return this.InProgressStateFiles.FirstOrDefault<StateFile>((Func<StateFile, bool>)(stateFile => revisionChangeSet.FileSetId == stateFile.FileSetId && revisionChangeSet.RevisionId == stateFile.InProgressRevisionId));
            }
        }
    }
}
