using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UpdateClientService.API.Services.DownloadService;
using UpdateClientService.API.Services.FileCache;

namespace UpdateClientService.API.Services.FileSets
{
    public class ChangeSetFileService : IChangeSetFileService
    {
        private readonly ILogger<ChangeSetFileService> _logger;
        private IFileSetRevisionDownloader _revisionDownloader;
        private readonly IFileSetDownloader _fileSetDownloader;
        private readonly IFileSetTransition _fileSetTransition;
        private readonly IFileCacheService _fileCacheService;
        private readonly IRevisionChangeSetRepository _revisionChangeSetRepository;

        public ChangeSetFileService(
          ILogger<ChangeSetFileService> logger,
          IFileSetRevisionDownloader revisionDownloader,
          IFileSetDownloader fileSetDownloader,
          IFileSetTransition fileSetTransition,
          IFileCacheService fileCacheService,
          IRevisionChangeSetRepository revisionChangeSetRepository)
        {
            this._logger = logger;
            this._revisionDownloader = revisionDownloader;
            this._fileSetDownloader = fileSetDownloader;
            this._fileSetTransition = fileSetTransition;
            this._fileCacheService = fileCacheService;
            this._revisionChangeSetRepository = revisionChangeSetRepository;
        }

        public async void CleanUp()
        {
            try
            {
                int num = await this._revisionChangeSetRepository.Cleanup(DateTime.Now.AddDays(-10.0)) ? 1 : 0;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "An unhandled exception occurred", nameof(CleanUp), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
            }
        }

        public async Task<bool> Delete(RevisionChangeSet revisionChangeSet)
        {
            return await this._revisionChangeSetRepository.Delete(revisionChangeSet);
        }

        public async Task<bool> CreateRevisionChangeSet(
          ClientFileSetRevisionChangeSet clientFileSetRevisionChangeSet)
        {
            bool result = false;
            try
            {
                result = await this._revisionChangeSetRepository.Save(RevisionChangeSet.Create(clientFileSetRevisionChangeSet));
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while creating RevisionChangeSet", nameof(CreateRevisionChangeSet), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
            }
            return result;
        }

        public async Task<List<RevisionChangeSet>> GetAllRevisionChangeSets()
        {
            return await this._revisionChangeSetRepository.GetAll();
        }

        public async Task ProcessChangeSet(
          DownloadDataList allDownloads,
          RevisionChangeSet revisionChangeSet)
        {
            try
            {
                DownloadDataList fileSetDownloads = allDownloads.GetByFileSetId(revisionChangeSet.FileSetId);
                await this.CheckReceived(revisionChangeSet, fileSetDownloads);
                await this.CheckDownloadingRevision(revisionChangeSet, fileSetDownloads);
                await this.CheckDownloadedRevision(revisionChangeSet, fileSetDownloads);
                await this.CheckDownloadingFileSet(revisionChangeSet, fileSetDownloads);
                await this.CheckDownloadedFileSet(revisionChangeSet);
                await this.CheckStagingFileSet(revisionChangeSet);
                await this.CheckStagedFileSet(revisionChangeSet);
                fileSetDownloads = (DownloadDataList)null;
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                string str = "Exception while processing RevisionChangeSet with " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(ProcessChangeSet), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
            }
        }

        public async Task ProcessActivationDependencyCheck(
          Dictionary<long, FileSetDependencyState> dependencyStates,
          RevisionChangeSet revisionChangeSet)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.ActivationDependencyCheck ? 1 : 0) : 1) != 0)
                return;
            this.LogRevisionChangeSetState(revisionChangeSet);
            try
            {
                ClientFileSetRevision clientFileSetRevision = await this.GetClientFileSetRevision(revisionChangeSet);
                if (clientFileSetRevision == null)
                {
                    await this.AttemptRetryAfterError("Revision does not exist", revisionChangeSet);
                    return;
                }
                bool flag = this._fileSetTransition.CheckMeetsDependency(clientFileSetRevision, dependencyStates);
                if (!flag)
                {
                    await this.AttemptRetryAfterError(FileSetState.NeedsDependency.ToString(), revisionChangeSet);
                    return;
                }
                if (flag)
                {
                    int num = await this.SetState(ChangesetState.ActivationPending, revisionChangeSet) ? 1 : 0;
                }
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                string str = "Exception while checking activation dependency for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(ProcessActivationDependencyCheck), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception", revisionChangeSet);
            }
        }

        public async Task ProcessActivationPending(RevisionChangeSet revisionChangeSet)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.ActivationPending ? 1 : 0) : 1) != 0)
                return;
            this.LogRevisionChangeSetState(revisionChangeSet);
            try
            {
                if (revisionChangeSet.ActiveOn > DateTime.Now)
                {
                    ILogger<ChangeSetFileService> logger = this._logger;
                    DateTime activeOn = revisionChangeSet.ActiveOn;
                    RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                    string str1 = revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null;
                    string str2 = string.Format("ActivateOn date {0} is in the future for {1}", (object)activeOn, (object)str1);
                    this._logger.LogInfoWithSource(str2, nameof(ProcessActivationPending), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                    return;
                }
                if (!this.IsInActivationTime(revisionChangeSet))
                {
                    this._logger.LogInfoWithSource("Current time is not within Activation time window.  ActivationStartTime: " + revisionChangeSet.ActivateStartTime + ", ActivationEndTime: " + revisionChangeSet.ActivateEndTime, nameof(ProcessActivationPending), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                    return;
                }
                int num = await this.SetState(ChangesetState.ActivationBeforeActions, revisionChangeSet) ? 1 : 0;
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                string str = "Exception while checking ActivationPending for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(ProcessActivationPending), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception", revisionChangeSet);
            }
        }

        public async Task ProcessActivationBeforeActions(RevisionChangeSet revisionChangeSet)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.ActivationBeforeActions ? 1 : 0) : 1) != 0)
                return;
            this.LogRevisionChangeSetState(revisionChangeSet);
            try
            {
                ClientFileSetRevision clientFileSetRevision = await this.GetClientFileSetRevision(revisionChangeSet);
                if (clientFileSetRevision == null)
                {
                    await this.AttemptRetryAfterError("Revision does not exist", revisionChangeSet);
                    return;
                }
                bool flag = this._fileSetTransition.BeforeActivate(clientFileSetRevision);
                if (!flag)
                {
                    await this.AttemptRetryAfterError("Error while running ActivationBeforeActions.", revisionChangeSet);
                    return;
                }
                if (flag)
                {
                    int num = await this.SetState(ChangesetState.Activating, revisionChangeSet) ? 1 : 0;
                }
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                string str = "Exception while checking ActivationBeforeActions for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(ProcessActivationBeforeActions), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception", revisionChangeSet);
            }
        }

        public async Task ProcessActivating(RevisionChangeSet revisionChangeSet)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.Activating ? 1 : 0) : 1) != 0)
                return;
            this.LogRevisionChangeSetState(revisionChangeSet);
            try
            {
                ClientFileSetRevision clientFileSetRevision = await this.GetClientFileSetRevision(revisionChangeSet);
                if (clientFileSetRevision == null)
                {
                    await this.AttemptRetryAfterError("Revision does not exist", revisionChangeSet);
                    return;
                }
                if (!this._fileSetTransition.Activate(clientFileSetRevision))
                {
                    await this.AttemptRetryAfterError("Activating revision failed", revisionChangeSet);
                    return;
                }
                int num = await this.SetState(ChangesetState.ActivationAfterActions, revisionChangeSet) ? 1 : 0;
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                string str = "Exception while checking Activating state for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(ProcessActivating), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception.", revisionChangeSet);
            }
        }

        public async Task ProcessActivationAfterActions(RevisionChangeSet revisionChangeSet)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.ActivationAfterActions ? 1 : 0) : 1) != 0)
                return;
            this.LogRevisionChangeSetState(revisionChangeSet);
            try
            {
                ClientFileSetRevision clientFileSetRevision = await this.GetClientFileSetRevision(revisionChangeSet);
                if (clientFileSetRevision == null)
                {
                    await this.AttemptRetryAfterError("Revision does not exist", revisionChangeSet);
                    return;
                }
                bool flag = this._fileSetTransition.AfterActivate(clientFileSetRevision);
                if (!flag)
                {
                    ILogger<ChangeSetFileService> logger = this._logger;
                    RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                    string str = "Errors activating RevisionChangeSet for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null) + ": " + flag.ToJson();
                    this._logger.LogWarningWithSource(str, nameof(ProcessActivationAfterActions), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                }
                int num = await this.SetState(ChangesetState.Activated, revisionChangeSet) ? 1 : 0;
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                string str = "Exception while checking ActivationAfterActions for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(ProcessActivationAfterActions), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                int num = await this.SetState(ChangesetState.Activated, revisionChangeSet) ? 1 : 0;
            }
        }

        private async Task CheckReceived(
          RevisionChangeSet revisionChangeSet,
          DownloadDataList downloadDataList)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != 0 ? 1 : 0) : 1) != 0)
                return;
            DownloadData revisionChangeSetKey1 = downloadDataList.GetByRevisionChangeSetKey((RevisionChangeSetKey)revisionChangeSet);
            this.LogRevisionChangeSetState(revisionChangeSet, revisionChangeSetKey1);
            try
            {
                if (this._revisionDownloader.DoesRevisionExist((RevisionChangeSetKey)revisionChangeSet))
                {
                    int num = await this.SetState(ChangesetState.DownloadedRevision, revisionChangeSet) ? 1 : 0;
                    return;
                }
                if (!this._revisionDownloader.DoesRevisionExist((RevisionChangeSetKey)revisionChangeSet) && revisionChangeSetKey1 == null)
                {
                    DownloadData downloadData = await this._revisionDownloader.AddDownload((RevisionChangeSetKey)revisionChangeSet, revisionChangeSet.FileHash, revisionChangeSet.Path, revisionChangeSet.DownloadPriority);
                    if (downloadData == null)
                    {
                        await this.AttemptRetryAfterError("RevisionDownloader.AddDownloader failed", revisionChangeSet);
                        return;
                    }
                    downloadDataList.Add(downloadData);
                }
                int num1 = await this.SetState(ChangesetState.DownloadingRevision, revisionChangeSet) ? 1 : 0;
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey2 = revisionChangeSet;
                string str = "Exception while checking received state for " + (revisionChangeSetKey2 != null ? revisionChangeSetKey2.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(CheckReceived), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception in CheckReceived", revisionChangeSet);
            }
        }

        private void LogRevisionChangeSetState(
          RevisionChangeSet revisionChangeSet,
          DownloadData downloadData = null)
        {
            string str = downloadData != null ? string.Format(";  DownloadState: {0}", (object)downloadData?.DownloadState) : string.Empty;
            this._logger.LogInfoWithSource(string.Format("RevisionChangeSet State = {0} for {1}{2}", (object)revisionChangeSet?.State, revisionChangeSet != null ? (object)revisionChangeSet.IdentifyingText() : (object)(string)null, (object)str), nameof(LogRevisionChangeSetState), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
        }

        private async Task CheckDownloadingRevision(
          RevisionChangeSet revisionChangeSet,
          DownloadDataList downloadDataList)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.DownloadingRevision ? 1 : 0) : 1) != 0)
                return;
            DownloadData revisionChangeSetKey1 = downloadDataList.GetByRevisionChangeSetKey((RevisionChangeSetKey)revisionChangeSet);
            this.LogRevisionChangeSetState(revisionChangeSet, revisionChangeSetKey1);
            try
            {
                if (this._revisionDownloader.DoesRevisionExist((RevisionChangeSetKey)revisionChangeSet))
                {
                    int num = await this.SetState(ChangesetState.DownloadedRevision, revisionChangeSet) ? 1 : 0;
                    return;
                }
                if (revisionChangeSetKey1 == null)
                {
                    await this.AttemptRetryAfterError("CheckDownloadingRevision - Download doesn't exist when it should", revisionChangeSet);
                    return;
                }
                if (this._revisionDownloader.IsDownloadError(revisionChangeSetKey1))
                {
                    await this.AttemptRetryAfterError("CheckDownloadingRevision - Download is in an error state", revisionChangeSet);
                    return;
                }
                if (this._revisionDownloader.IsDownloadComplete((RevisionChangeSetKey)revisionChangeSet, revisionChangeSetKey1))
                {
                    if (this._revisionDownloader.CompleteDownload((RevisionChangeSetKey)revisionChangeSet, revisionChangeSetKey1))
                    {
                        int num1 = await this.SetState(ChangesetState.DownloadedRevision, revisionChangeSet) ? 1 : 0;
                    }
                    else
                        await this.AttemptRetryAfterError("CheckDownloadingRevision - CompleteDownload failed", revisionChangeSet);
                }
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey2 = revisionChangeSet;
                string str = "Exception while checking DownloadingRevision state for " + (revisionChangeSetKey2 != null ? revisionChangeSetKey2.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(CheckDownloadingRevision), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception in CheckDownloadingRevision", revisionChangeSet);
            }
        }

        private async Task<ClientFileSetRevision> GetClientFileSetRevision(
          RevisionChangeSet revisionChangeSet)
        {
            return await this._fileCacheService.GetClientFileSetRevision((RevisionChangeSetKey)revisionChangeSet);
        }

        private async Task CheckDownloadedRevision(
          RevisionChangeSet revisionChangeSet,
          DownloadDataList downloadDataList)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.DownloadedRevision ? 1 : 0) : 1) != 0)
                return;
            DownloadData revisionChangeSetKey1 = downloadDataList.GetByRevisionChangeSetKey((RevisionChangeSetKey)revisionChangeSet);
            this.LogRevisionChangeSetState(revisionChangeSet, revisionChangeSetKey1);
            try
            {
                ClientFileSetRevision clientFileSetRevision = await this.GetClientFileSetRevision(revisionChangeSet);
                if (clientFileSetRevision == null)
                {
                    await this.AttemptRetryAfterError("Revision does not exist", revisionChangeSet);
                    return;
                }
                if (this._fileSetDownloader.IsDownloaded(clientFileSetRevision))
                {
                    int num = await this.SetState(ChangesetState.DownloadedFileSet, revisionChangeSet) ? 1 : 0;
                    return;
                }
                if (await this._fileSetDownloader.DownloadFileSet(clientFileSetRevision, downloadDataList, revisionChangeSet.DownloadPriority))
                {
                    int num1 = await this.SetState(ChangesetState.DownloadingFileSet, revisionChangeSet) ? 1 : 0;
                }
                else
                    await this.AttemptRetryAfterError("An error while starting fileset downloads", revisionChangeSet);
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey2 = revisionChangeSet;
                string str = "Exception while checking DownloadedRevsion state for " + (revisionChangeSetKey2 != null ? revisionChangeSetKey2.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(CheckDownloadedRevision), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception in CheckDownloadedRevision", revisionChangeSet);
            }
        }

        private async Task CheckDownloadingFileSet(
          RevisionChangeSet revisionChangeSet,
          DownloadDataList downloadDataList)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.DownloadingFileSet ? 1 : 0) : 1) != 0)
                return;
            DownloadData revisionChangeSetKey1 = downloadDataList.GetByRevisionChangeSetKey((RevisionChangeSetKey)revisionChangeSet);
            this.LogRevisionChangeSetState(revisionChangeSet, revisionChangeSetKey1);
            try
            {
                ClientFileSetRevision clientFileSetRevision = await this.GetClientFileSetRevision(revisionChangeSet);
                if (clientFileSetRevision == null)
                {
                    await this.AttemptRetryAfterError("Revision does not exist", revisionChangeSet);
                    return;
                }
                if (downloadDataList == null || downloadDataList.Count == 0)
                {
                    await this.AttemptRetryAfterError("Download does not exist for FileSet", revisionChangeSet);
                    return;
                }
                if (!this._fileSetDownloader.CompleteDownloads(clientFileSetRevision, downloadDataList))
                {
                    await this.AttemptRetryAfterError("Complete downloads failed", revisionChangeSet);
                    return;
                }
                if (downloadDataList.IsDownloading)
                    return;
                if (this._fileSetDownloader.IsDownloaded(clientFileSetRevision))
                {
                    int num = await this.SetState(ChangesetState.DownloadedFileSet, revisionChangeSet) ? 1 : 0;
                }
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey2 = revisionChangeSet;
                string str = "Exception  while checking DownloadingFileSet state for " + (revisionChangeSetKey2 != null ? revisionChangeSetKey2.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(CheckDownloadingFileSet), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception in CheckDownloadingFileSet", revisionChangeSet);
            }
        }

        private async Task CheckDownloadedFileSet(RevisionChangeSet revisionChangeSet)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.DownloadedFileSet ? 1 : 0) : 1) != 0)
                return;
            this.LogRevisionChangeSetState(revisionChangeSet);
            try
            {
                int num = await this.SetState(ChangesetState.Staging, revisionChangeSet) ? 1 : 0;
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                string str = "Exception while checking DownloadedFileSet state for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(CheckDownloadedFileSet), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception in CheckDownloadedFileSet", revisionChangeSet);
            }
        }

        private async Task CheckStagingFileSet(RevisionChangeSet revisionChangeSet)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.Staging ? 1 : 0) : 1) != 0)
                return;
            this.LogRevisionChangeSetState(revisionChangeSet);
            try
            {
                ClientFileSetRevision clientFileSetRevision = await this.GetClientFileSetRevision(revisionChangeSet);
                if (clientFileSetRevision == null)
                {
                    await this.AttemptRetryAfterError("Revision does not exist", revisionChangeSet);
                    return;
                }
                if (!this._fileSetTransition.Stage(clientFileSetRevision))
                {
                    await this.AttemptRetryAfterError("Staging revision failed.", revisionChangeSet);
                    return;
                }
                int num = await this.SetState(ChangesetState.Staged, revisionChangeSet) ? 1 : 0;
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                string str = "Exception while checking Staging state for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(CheckStagingFileSet), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception in CheckStagingFileSet", revisionChangeSet);
            }
        }

        private async Task CheckStagedFileSet(RevisionChangeSet revisionChangeSet)
        {
            RevisionChangeSet revisionChangeSet1 = revisionChangeSet;
            if ((revisionChangeSet1 != null ? (revisionChangeSet1.State != ChangesetState.Staged ? 1 : 0) : 1) != 0)
                return;
            this.LogRevisionChangeSetState(revisionChangeSet);
            try
            {
                int num = await this.SetState(ChangesetState.ActivationDependencyCheck, revisionChangeSet) ? 1 : 0;
            }
            catch (Exception ex)
            {
                ILogger<ChangeSetFileService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSet revisionChangeSetKey = revisionChangeSet;
                string str = "Exception while checking Staged state for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(CheckStagedFileSet), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                await this.AttemptRetryAfterError("Unhandled exception in CheckStagedFileSet", revisionChangeSet);
            }
        }

        private async Task<bool> SetState(ChangesetState state, RevisionChangeSet revisionChangeSet)
        {
            revisionChangeSet.State = state;
            return await this._revisionChangeSetRepository.Save(revisionChangeSet);
        }

        private bool IsInActivationTime(RevisionChangeSet revisionChangeSet)
        {
            TimeSpan result1;
            TimeSpan result2;
            if (string.IsNullOrEmpty(revisionChangeSet.ActivateStartTime) || string.IsNullOrEmpty(revisionChangeSet.ActivateEndTime) || !TimeSpan.TryParse(revisionChangeSet.ActivateStartTime, out result1) || !TimeSpan.TryParse(revisionChangeSet.ActivateEndTime, out result2) || result1 == result2)
                return true;
            DateTime now = DateTime.Now;
            return result2 < result1 ? now.TimeOfDay <= result2 || now.TimeOfDay >= result1 : now.TimeOfDay >= result1 && now.TimeOfDay <= result2;
        }

        private async Task AttemptRetryAfterError(
          string errorMessage,
          RevisionChangeSet revisionChangeSet)
        {
            this._logger.LogErrorWithSource("Error for RevisionChangeSet with " + revisionChangeSet.IdentifyingText() + " (" + errorMessage + ")", nameof(AttemptRetryAfterError), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
            revisionChangeSet.Message = errorMessage;
            ++revisionChangeSet.RetryCount;
            if (revisionChangeSet.RetryCount > 2)
            {
                this._logger.LogInfoWithSource("Setting Error state for RevsionChangeSet with " + revisionChangeSet.IdentifyingText(), nameof(AttemptRetryAfterError), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                int num = await this.SetState(ChangesetState.Error, revisionChangeSet) ? 1 : 0;
            }
            else
            {
                this._logger.LogInfoWithSource("Retrying download of RevisionChangeSet with " + revisionChangeSet.IdentifyingText(), nameof(AttemptRetryAfterError), "/sln/src/UpdateClientService.API/Services/FileSets/ChangeSetFileService.cs");
                int num = await this.SetState(ChangesetState.Received, revisionChangeSet) ? 1 : 0;
            }
        }
    }
}
