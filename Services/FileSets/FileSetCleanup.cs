using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UpdateClientService.API.Services.FileCache;

namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetCleanup : IFileSetCleanup
    {
        private readonly IFileCacheService _fileCacheService;
        private readonly IFileSetService _fileSetService;
        private readonly IStateFileService _stateFileService;
        private readonly ILogger<FileSetCleanup> _logger;

        public FileSetCleanup(
          IFileCacheService fileCacheService,
          IFileSetService fileSetService,
          IStateFileService stateFileService,
          ILogger<FileSetCleanup> logger)
        {
            this._fileCacheService = fileCacheService;
            this._fileSetService = fileSetService;
            this._stateFileService = stateFileService;
            this._logger = logger;
        }

        public async Task Run()
        {
            try
            {
                FileSetCleanup.CleanupData fileSetCleanupData = new FileSetCleanup.CleanupData();
                await this.GetStateFiles(fileSetCleanupData);
                foreach (long fileSetId in this._fileCacheService.GetFileSetIds())
                    await this.CleanupFileSet(fileSetCleanupData, fileSetId);
                fileSetCleanupData = (FileSetCleanup.CleanupData)null;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while cleaning up FileSets", nameof(Run), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetCleanup.cs");
            }
        }

        private async Task CleanupFileSet(FileSetCleanup.CleanupData cleanupData, long fileSetId)
        {
            if (cleanupData.IsComplete)
                return;
            this._logger.LogInfoWithSource(string.Format("FileSet Cleanup for FileSetId {0}", (object)fileSetId), nameof(CleanupFileSet), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetCleanup.cs");
            FileSetCleanup.FileSetCleanupData fileSetCleanupData = new FileSetCleanup.FileSetCleanupData()
            {
                CleanupData = cleanupData,
                FileSetId = fileSetId
            };
            this.GetActiveOrInProgressFileSetRevisionIds(fileSetCleanupData);
            await this.GetUsedClientFileSetRevisions(fileSetCleanupData);
            this.GetRetentionCriteria(fileSetCleanupData);
            await this.GetFileSetFileInfos(fileSetCleanupData);
            await this.CleanupFileSetRevisions(fileSetCleanupData, fileSetId);
            this.RemoveUnneededRevisionFiles(fileSetCleanupData);
            fileSetCleanupData = (FileSetCleanup.FileSetCleanupData)null;
        }

        private async Task CleanupFileSetRevisions(
          FileSetCleanup.FileSetCleanupData fileSetCleanupData,
          long fileSetId)
        {
            if (fileSetCleanupData.IsComplete)
                return;
            foreach (long num in this._fileCacheService.GetFileSetRevisionIds(fileSetId).OrderByDescending<long, long>((Func<long, long>)(x => x)).ToList<long>())
            {
                bool revisionRetained = this.IsRevisionRetained(fileSetCleanupData, num);
                foreach (string revisionFilePath in this._fileCacheService.GetClientFileSetRevisionFilePaths(fileSetId, num))
                {
                    ClientFileSetRevision clientFileSetRevision = await this._fileCacheService.GetClientFileSetRevision(revisionFilePath);
                    if (clientFileSetRevision != null)
                    {
                        if (revisionRetained)
                            this.AddRequiredFiles(fileSetCleanupData, clientFileSetRevision);
                        else
                            this._fileCacheService.DeleteRevision((RevisionChangeSetKey)clientFileSetRevision);
                    }
                }
                if (revisionRetained)
                    ++fileSetCleanupData.RetainedRevisions;
            }
        }

        private bool IsRevisionRetained(
          FileSetCleanup.FileSetCleanupData fileSetCleanupData,
          long fileSetRevisionId)
        {
            if (fileSetCleanupData.ActiveOrInProgressFileSetRevisionIds.Any<long>((Func<long, bool>)(x => x == fileSetRevisionId)))
                return true;
            double totalDays = (DateTime.Now - this._fileCacheService.GetRevisionCreationDate(fileSetCleanupData.FileSetId, fileSetRevisionId)).TotalDays;
            if (totalDays <= 1.0)
                return true;
            if (fileSetCleanupData.RetentionDays > 0 && totalDays > (double)fileSetCleanupData.RetentionDays)
            {
                this._logger.LogInfoWithSource(string.Format("FileSetId {0} FileSetRevisionId {1} has exceeded the retention {2} days and will be deleted", (object)fileSetCleanupData.FileSetId, (object)fileSetRevisionId, (object)fileSetCleanupData.RetentionDays), nameof(IsRevisionRetained), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetCleanup.cs");
                return false;
            }
            if (fileSetCleanupData.RetentionRevisions <= 0 || fileSetCleanupData.RetainedRevisions < fileSetCleanupData.RetentionRevisions)
                return true;
            this._logger.LogInfoWithSource(string.Format("FileSetId {0} FileSetRevisionId {1} has exceeded the retention count {2} and will be deleted", (object)fileSetCleanupData.FileSetId, (object)fileSetRevisionId, (object)fileSetCleanupData.RetentionRevisions), nameof(IsRevisionRetained), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetCleanup.cs");
            return false;
        }

        private async Task GetFileSetFileInfos(
          FileSetCleanup.FileSetCleanupData fileSetCleanupData)
        {
            if (fileSetCleanupData.IsComplete)
                return;
            FileSetCleanup.FileSetCleanupData fileSetCleanupData1 = fileSetCleanupData;
            fileSetCleanupData1.FileSetFileInfos = await this._fileCacheService.GetFileInfos(fileSetCleanupData.FileSetId);
            fileSetCleanupData1 = (FileSetCleanup.FileSetCleanupData)null;
        }

        private void GetRetentionCriteria(
          FileSetCleanup.FileSetCleanupData fileSetCleanupData)
        {
            if (fileSetCleanupData.IsComplete)
                return;
            long maxFileSetRevisionId = fileSetCleanupData.UsedClientFileSetRevisions.Max<ClientFileSetRevision>((Func<ClientFileSetRevision, long>)(x => x.RevisionId));
            ClientFileSetRevision clientFileSetRevision = fileSetCleanupData.UsedClientFileSetRevisions.FirstOrDefault<ClientFileSetRevision>((Func<ClientFileSetRevision, bool>)(x => x.RevisionId == maxFileSetRevisionId));
            fileSetCleanupData.RetentionDays = clientFileSetRevision.RetentionDays;
            fileSetCleanupData.RetentionRevisions = clientFileSetRevision.RetentionRevisions;
            if (fileSetCleanupData.RetentionDays != 0 || fileSetCleanupData.RetentionRevisions != 0)
                return;
            fileSetCleanupData.IsComplete = true;
        }

        private async Task GetUsedClientFileSetRevisions(
          FileSetCleanup.FileSetCleanupData fileSetCleanupData)
        {
            if (fileSetCleanupData.IsComplete)
                return;
            foreach (long fileSetRevisionId in fileSetCleanupData.ActiveOrInProgressFileSetRevisionIds)
            {
                ClientFileSetRevision clientFileSetRevision = await this._fileCacheService.GetAnyClientFileSetRevision(fileSetCleanupData.FileSetId, fileSetRevisionId);
                if (clientFileSetRevision != null)
                    fileSetCleanupData.UsedClientFileSetRevisions.Add(clientFileSetRevision);
            }
            if (fileSetCleanupData.UsedClientFileSetRevisions.Any<ClientFileSetRevision>())
                return;
            fileSetCleanupData.IsComplete = true;
        }

        private void GetActiveOrInProgressFileSetRevisionIds(
          FileSetCleanup.FileSetCleanupData fileSetCleanupData)
        {
            fileSetCleanupData.StateFile = fileSetCleanupData.CleanupData.StateFiles.FirstOrDefault<StateFile>((Func<StateFile, bool>)(x => x.FileSetId == fileSetCleanupData.FileSetId));
            if (fileSetCleanupData.StateFile != null)
            {
                if (fileSetCleanupData.StateFile.HasActiveRevision)
                    fileSetCleanupData.ActiveOrInProgressFileSetRevisionIds.Add(fileSetCleanupData.StateFile.ActiveRevisionId);
                if (fileSetCleanupData.StateFile.IsRevisionDownloadInProgress)
                    fileSetCleanupData.ActiveOrInProgressFileSetRevisionIds.Add(fileSetCleanupData.StateFile.InProgressRevisionId);
            }
            fileSetCleanupData.ActiveOrInProgressFileSetRevisionIds = fileSetCleanupData.ActiveOrInProgressFileSetRevisionIds.Distinct<long>().ToList<long>();
            if (fileSetCleanupData.ActiveOrInProgressFileSetRevisionIds.Any<long>())
                return;
            fileSetCleanupData.IsComplete = true;
        }

        private async Task GetStateFiles(FileSetCleanup.CleanupData fileSetCleanupData)
        {
            StateFilesResponse all = await this._stateFileService.GetAll();
            if (all.StatusCode == HttpStatusCode.InternalServerError)
            {
                this._logger.LogWarningWithSource("Unable to get StateFiles", nameof(GetStateFiles), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetCleanup.cs");
                fileSetCleanupData.IsComplete = true;
            }
            else
                fileSetCleanupData.StateFiles = all.StateFiles;
        }

        private void AddRequiredFiles(
          FileSetCleanup.FileSetCleanupData fileSetCleanupData,
          ClientFileSetRevision clientFileSetRevision)
        {
            List<FileSetFileInfo> list1 = fileSetCleanupData.FileSetFileInfos.Where<FileSetFileInfo>((Func<FileSetFileInfo, bool>)(x => clientFileSetRevision.Files.Any<ClientFileSetFile>((Func<ClientFileSetFile, bool>)(y => y.FileId == x.FileId && y.FileRevisionId == x.FileRevisionId)))).ToList<FileSetFileInfo>();
            List<FileSetFileInfo> list2 = fileSetCleanupData.FileSetFileInfos.Where<FileSetFileInfo>((Func<FileSetFileInfo, bool>)(x => clientFileSetRevision.PatchFiles.Any<ClientPatchFileSetFile>((Func<ClientPatchFileSetFile, bool>)(y => y.FileId == x.FileId && y.PatchFileRevisionId == x.FileRevisionId)))).ToList<FileSetFileInfo>();
            fileSetCleanupData.RequiredFileSetFileInfos.AddRange((IEnumerable<FileSetFileInfo>)list1);
            fileSetCleanupData.RequiredFileSetFileInfos.AddRange((IEnumerable<FileSetFileInfo>)list2);
        }

        private void RemoveUnneededRevisionFiles(
          FileSetCleanup.FileSetCleanupData fileSetCleanupData)
        {
            fileSetCleanupData.FileSetFileInfos.Except<FileSetFileInfo>(fileSetCleanupData.RequiredFileSetFileInfos.Distinct<FileSetFileInfo>()).ToList<FileSetFileInfo>().ForEach((Action<FileSetFileInfo>)(eachFileToRemove =>
            {
                this._logger.LogInfoWithSource(string.Format("Deleting FileSetId: {0} FileId {1} FileRevisionId {2}", (object)fileSetCleanupData.FileSetId, (object)eachFileToRemove.FileId, (object)eachFileToRemove.FileRevisionId), nameof(RemoveUnneededRevisionFiles), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetCleanup.cs");
                this._fileCacheService.DeleteFile(fileSetCleanupData.FileSetId, eachFileToRemove.FileId, eachFileToRemove.FileRevisionId);
            }));
        }

        private class CleanupData
        {
            public List<StateFile> StateFiles { get; set; } = new List<StateFile>();

            public bool IsComplete { get; set; }
        }

        private class FileSetCleanupData
        {
            public long FileSetId { get; set; }

            public FileSetCleanup.CleanupData CleanupData { get; set; }

            public StateFile StateFile { get; set; }

            public List<long> ActiveOrInProgressFileSetRevisionIds { get; set; } = new List<long>();

            public List<ClientFileSetRevision> UsedClientFileSetRevisions { get; set; } = new List<ClientFileSetRevision>();

            public List<FileSetFileInfo> FileSetFileInfos { get; set; } = new List<FileSetFileInfo>();

            public List<FileSetFileInfo> RequiredFileSetFileInfos { get; set; } = new List<FileSetFileInfo>();

            public int RetentionDays { get; set; }

            public int RetentionRevisions { get; set; }

            public int RetainedRevisions { get; set; }

            public bool IsComplete { get; set; }
        }
    }
}
