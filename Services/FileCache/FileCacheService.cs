using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UpdateClientService.API.Services.FileSets;

namespace UpdateClientService.API.Services.FileCache
{
    public class FileCacheService : IFileCacheService
    {
        private readonly ILogger<FileCacheService> _logger;

        public FileCacheService(ILogger<FileCacheService> logger) => this._logger = logger;

        public bool DoesFileExist(long fileSetId, long fileId, long fileRevisionId)
        {
            try
            {
                return File.Exists(this.GetRevisionFilePath(fileSetId, fileId, fileRevisionId));
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while checking the existance of File with FileSetId {0}, FileId {1}, FileRevisionId {2}", (object)fileSetId, (object)fileId, (object)fileRevisionId), nameof(DoesFileExist), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return false;
        }

        public bool IsFileHashValid(long fileSetId, long fileId, long fileRevisionId, string fileHash)
        {
            try
            {
                return this.IsFileHashValid(this.GetRevisionFilePath(fileSetId, fileId, fileRevisionId), fileHash);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while checking the hash for File with FileSetId {0}, FileId {1}, FileRevisionId {2}", (object)fileSetId, (object)fileId, (object)fileRevisionId), nameof(IsFileHashValid), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return false;
        }

        public bool IsFileHashValid(string filePath, string hash)
        {
            try
            {
                using (FileStream fileStream = File.OpenRead(filePath))
                    return ((Stream)fileStream).GetSHA1Hash() == hash;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Exception while checking hash for file " + filePath + ".", Array.Empty<object>());
            }
            return false;
        }

        public bool DoesRevisionExist(RevisionChangeSetKey revisionChangeSetKey)
        {
            try
            {
                return File.Exists(this.GetRevisionPath(revisionChangeSetKey));
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while checking the existence of a revsion for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null), nameof(DoesRevisionExist), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return false;
        }

        public bool AddFile(
          long fileSetId,
          long fileId,
          long fileRevisionId,
          byte[] data,
          string info)
        {
            bool flag = false;
            try
            {
                this.CheckFilePath(fileSetId, fileId);
                File.WriteAllBytes(this.GetRevisionFilePath(fileSetId, fileId, fileRevisionId), data);
                File.WriteAllText(this.GetFileInfoPath(fileSetId, fileId, fileRevisionId), info);
                this._logger.LogInformation("FileCacheService.AddFile - Info: " + info, Array.Empty<object>());
                flag = true;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while adding file FileSetId {0}, FileId {1}, FileRevisionId {2}", (object)fileSetId, (object)fileId, (object)fileRevisionId), nameof(AddFile), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return flag;
        }

        public bool AddFilePatch(
          long fileSetId,
          long fileId,
          long fileRevisionId,
          long filePatchRevisionId,
          byte[] data,
          string info)
        {
            bool flag = false;
            try
            {
                this.CheckFilePath(fileSetId, fileId);
                if (!this.GetFile(fileSetId, fileId, filePatchRevisionId, out byte[] _))
                {
                    this._logger.LogError(string.Format("Patch source file is missing FileId: {0} FileRevisionId {1}", (object)fileId, (object)filePatchRevisionId), Array.Empty<object>());
                    return flag;
                }
                string revisionFilePath = this.GetRevisionFilePath(fileSetId, fileId, filePatchRevisionId);
                List<Error> errorList = new List<Error>();
                using (FileStream source = new FileStream(revisionFilePath, (FileMode)3, (FileAccess)1))
                {
                    ((Stream)source).Position = 0L;
                    using (MemoryStream patch = new MemoryStream(data))
                    {
                        ((Stream)patch).Position = 0L;
                        using (FileStream target = new FileStream(this.GetRevisionFilePath(fileSetId, fileId, fileRevisionId), (FileMode)4, (FileAccess)2))
                            errorList.AddRange((IEnumerable<Error>)XDeltaHelper.Apply((Stream)source, (Stream)patch, (Stream)target));
                    }
                }
                File.WriteAllText(this.GetFileInfoPath(fileSetId, fileId, fileRevisionId), info);
                this._logger.LogInfoWithSource("FileCacheService.AddFilePatch - Info: " + info, nameof(AddFilePatch), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
                if (errorList.Count > 0)
                    this._logger.LogErrorWithSource(errorList[0].Message, nameof(AddFilePatch), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
                else
                    flag = true;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while adding File Patch with FileSetId {0}, FileId {1}, FileRevisionId {2}, FilePatchRevisionId {3}", (object)fileSetId, (object)fileId, (object)fileRevisionId, (object)filePatchRevisionId), nameof(AddFilePatch), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return flag;
        }

        public bool AddRevision(RevisionChangeSetKey revisionChangeSetKey, byte[] data, string info)
        {
            bool flag = false;
            try
            {
                this.CheckFileSetPath(revisionChangeSetKey.FileSetId);
                File.WriteAllBytes(this.GetRevisionPath(revisionChangeSetKey), data);
                File.WriteAllText(this.GetRevisionInfoPath(revisionChangeSetKey), info);
                this._logger.LogInfoWithSource("Adding Revision: " + info, nameof(AddRevision), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
                flag = true;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while adding revision for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null), nameof(AddRevision), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return flag;
        }

        public void DeleteFile(long fileSetId, long fileId, long fileRevisionId, bool force = false)
        {
            try
            {
                string revisionFilePath = this.GetRevisionFilePath(fileSetId, fileId, fileRevisionId);
                if (!force && (DateTime.Now - File.GetCreationTime(revisionFilePath)).TotalDays <= 1.0)
                {
                    this._logger.LogInfoWithSource(string.Format("Cannot delete files less than a day old - FileSetId {0}, FileId {1}, FileRevisionId {2}", (object)fileSetId, (object)fileId, (object)fileRevisionId), nameof(DeleteFile), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
                }
                else
                {
                    this._logger.LogInfoWithSource(string.Format("Deleting file for FileSetId {0}, FileId {1}, FileRevisionId {2}", (object)fileSetId, (object)fileId, (object)fileRevisionId), nameof(DeleteFile), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
                    this.DeleteFileIfExists(revisionFilePath);
                    this.DeleteFileIfExists(this.GetFileInfoPath(fileSetId, fileId, fileRevisionId));
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while deleting File for FileSetId {0}, FileId {1}, FileRevisionId {2}", (object)fileSetId, (object)fileId, (object)fileRevisionId), nameof(DeleteFile), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
        }

        private void DeleteFileIfExists(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while deleting file " + filePath, nameof(DeleteFileIfExists), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
        }

        public void DeleteRevision(RevisionChangeSetKey revisionChangeSetKey)
        {
            try
            {
                this._logger.LogInfoWithSource("Deleting revision with " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null), nameof(DeleteRevision), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
                this.DeleteFileIfExists(this.GetRevisionPath(revisionChangeSetKey));
                this.DeleteFileIfExists(this.GetRevisionInfoPath(revisionChangeSetKey));
                string stagingDirectory = this.GetStagingDirectory(revisionChangeSetKey.FileSetId, revisionChangeSetKey.RevisionId);
                if (!Directory.Exists(stagingDirectory))
                    return;
                Directory.Delete(stagingDirectory, true);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while deleting revision with " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null), nameof(DeleteRevision), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
        }

        public bool GetFile(long fileSetId, long fileId, long fileRevisionId, out byte[] data)
        {
            data = (byte[])null;
            List<Error> errorList = new List<Error>();
            try
            {
                string revisionFilePath = this.GetRevisionFilePath(fileSetId, fileId, fileRevisionId);
                if (File.Exists(revisionFilePath))
                {
                    data = File.ReadAllBytes(revisionFilePath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorList.Add(new Error()
                {
                    Message = string.Format("FileCacheService.GetFile Unhandled exception occurred. {0}", (object)ex)
                });
            }
            return false;
        }

        public async Task<List<FileSetFileInfo>> GetFileInfos(long fileSetId)
        {
            List<FileSetFileInfo> result = new List<FileSetFileInfo>();
            foreach (string fileInfoPath in this.GetFileInfoPaths(fileSetId))
            {
                FileSetFileInfo fileInfo = await this.GetFileInfo(fileInfoPath);
                if (fileInfo != null)
                    result.Add(fileInfo);
            }
            return result;
        }

        public async Task<FileSetFileInfo> GetFileInfo(string filePath)
        {
            FileSetFileInfo result = (FileSetFileInfo)null;
            try
            {
                (bool flag, byte[] numArray) = await this.ReadFileBytes(filePath);
                if (flag)
                    result = Encoding.ASCII.GetString(numArray).ToObject<FileSetFileInfo>();
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while getting FileSetFileInfo from file " + filePath, nameof(GetFileInfo), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return result;
        }

        public bool CopyFileToPath(
          long fileSetId,
          long fileId,
          long fileRevisionId,
          string copyToPath)
        {
            try
            {
                string revisionFilePath = this.GetRevisionFilePath(fileSetId, fileId, fileRevisionId);
                if (File.Exists(revisionFilePath))
                {
                    File.Copy(revisionFilePath, copyToPath, true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while copying file FileSetId {0}, FileId {1}, FileRevisionId {2}, CopyToPath {3}", (object)fileSetId, (object)fileId, (object)fileRevisionId, (object)copyToPath), nameof(CopyFileToPath), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return false;
        }

        public async Task<ClientFileSetRevision> GetAnyClientFileSetRevision(
          long fileSetId,
          long fileSetRevisionId)
        {
            ClientFileSetRevision result = (ClientFileSetRevision)null;
            try
            {
                result = await this.GetClientFileSetRevision(this.GetClientFileSetRevisionFilePaths(fileSetId, fileSetRevisionId).FirstOrDefault<string>());
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Exception while getting ClientFileSetRevision for FileSetId {0} and FileSetRevisionId {1}", (object)fileSetId, (object)fileSetRevisionId), nameof(GetAnyClientFileSetRevision), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return result;
        }

        public async Task<ClientFileSetRevision> GetClientFileSetRevision(
          RevisionChangeSetKey revisionChangeSetKey)
        {
            ClientFileSetRevision result = (ClientFileSetRevision)null;
            try
            {
                result = await this.GetClientFileSetRevision(this.GetRevisionPath(revisionChangeSetKey));
            }
            catch (Exception ex)
            {
                ILogger<FileCacheService> logger = this._logger;
                Exception exception = ex;
                RevisionChangeSetKey revisionChangeSetKey1 = revisionChangeSetKey;
                string str = "Exception while reading ClienfFileSetRevision for " + (revisionChangeSetKey1 != null ? revisionChangeSetKey1.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(GetClientFileSetRevision), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return result;
        }

        public async Task<ClientFileSetRevision> GetClientFileSetRevision(string filePath)
        {
            ClientFileSetRevision result = (ClientFileSetRevision)null;
            try
            {
                (bool flag, byte[] numArray) = await this.ReadFileBytes(filePath);
                if (flag)
                    result = Encoding.ASCII.GetString(numArray).ToObject<ClientFileSetRevision>();
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while reading ClienfFileSetRevision from file " + filePath, nameof(GetClientFileSetRevision), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return result;
        }

        private async Task<(bool, byte[])> ReadFileBytes(string filePath)
        {
            bool result = false;
            byte[] fileBytes = (byte[])null;
            try
            {
                if (File.Exists(filePath))
                {
                    fileBytes = File.ReadAllBytes(filePath);
                    result = true;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while reading file " + filePath, nameof(ReadFileBytes), "/sln/src/UpdateClientService.API/Services/FileCache/FileCacheService.cs");
            }
            return (result, fileBytes);
        }

        public List<string> GetClientFileSetRevisionFilePaths(long fileSetId)
        {
            List<string> revisionFilePaths = new List<string>();
            string fileSetDirectory = this.GetFileSetDirectory(fileSetId);
            if (Directory.Exists(fileSetDirectory))
                revisionFilePaths = ((IEnumerable<string>)Directory.GetFiles(fileSetDirectory, "*.revision", SearchOption.TopDirectoryOnly)).ToList<string>();
            return revisionFilePaths;
        }

        public List<long> GetFileSetIds()
        {
            List<long> fileSetIds = new List<long>();
            if (!Directory.Exists(UpdateClientService.API.Services.FileSets.Constants.FileCacheRootPath))
                return fileSetIds;
            foreach (string directory in Directory.GetDirectories(UpdateClientService.API.Services.FileSets.Constants.FileCacheRootPath))
            {
                string fileName = Path.GetFileName(directory);
                try
                {
                    fileSetIds.Add(Convert.ToInt64(fileName));
                }
                catch
                {
                }
            }
            return fileSetIds;
        }

        public List<long> GetFileSetRevisionIds(long fileSetId)
        {
            List<long> source = new List<long>();
            foreach (string revisionFilePath in this.GetClientFileSetRevisionFilePaths(fileSetId))
            {
                (bool success, long revisionId) = this.ParseRevisionIdFromRevisionFileName(Path.GetFileName(revisionFilePath));
                if (success)
                    source.Add(revisionId);
            }
            return source.Distinct<long>().ToList<long>();
        }

        private (bool success, long revisionId) ParseRevisionIdFromRevisionFileName(
          string revisionFileName)
        {
            bool flag = false;
            long result = 0;
            if (!string.IsNullOrEmpty(revisionFileName) && revisionFileName.Contains("-"))
            {
                string[] source = revisionFileName.Split('-');
                if (source != null && ((IEnumerable<string>)source).Count<string>() > 0 && long.TryParse(source[0], out result))
                    flag = true;
            }
            return (flag, result);
        }

        public List<string> GetClientFileSetRevisionFilePaths(long fileSetId, long revisionId)
        {
            List<string> revisionFilePaths = new List<string>();
            string fileSetDirectory = this.GetFileSetDirectory(fileSetId);
            if (Directory.Exists(fileSetDirectory))
                revisionFilePaths = ((IEnumerable<string>)Directory.GetFiles(fileSetDirectory, string.Format("{0}-*.revision", (object)revisionId), SearchOption.TopDirectoryOnly)).ToList<string>();
            return revisionFilePaths;
        }

        public DateTime GetRevisionCreationDate(long fileSetId, long revisionId)
        {
            return this.GetMaxCreationdDate(this.GetClientFileSetRevisionFilePaths(fileSetId, revisionId));
        }

        private DateTime GetMaxCreationdDate(List<string> filePaths)
        {
            DateTime maxCreationdDate = DateTime.MinValue;
            foreach (string filePath in filePaths)
            {
                DateTime creationTime = File.GetCreationTime(filePath);
                if (creationTime > maxCreationdDate)
                    maxCreationdDate = creationTime;
            }
            return maxCreationdDate;
        }

        public List<string> GetFileInfoPaths(long fileSetId)
        {
            List<string> fileInfoPaths = new List<string>();
            string revisionFileDirectory = this.GetRevisionFileDirectory(fileSetId);
            if (Directory.Exists(revisionFileDirectory))
                fileInfoPaths = ((IEnumerable<string>)Directory.GetFiles(revisionFileDirectory, "*.fileinfo", SearchOption.TopDirectoryOnly)).ToList<string>();
            return fileInfoPaths;
        }

        private string GetFileSetDirectory(long fileSetId)
        {
            return Path.Combine(UpdateClientService.API.Services.FileSets.Constants.FileCacheRootPath, string.Format("{0}", (object)fileSetId));
        }

        private string GetRevisionPath(RevisionChangeSetKey revisionChangeSetKey)
        {
            return this.GetRevisionPath(revisionChangeSetKey != null ? revisionChangeSetKey.FileSetId : 0L, revisionChangeSetKey != null ? revisionChangeSetKey.RevisionId : 0L, revisionChangeSetKey != null ? revisionChangeSetKey.PatchRevisionId : 0L);
        }

        private string GetRevisionPath(long fileSetId, long revisionId, long patchRevisionId)
        {
            return Path.Combine(this.GetFileSetDirectory(fileSetId), this.GetRevisionName(revisionId, patchRevisionId));
        }

        private string GetRevisionInfoPath(RevisionChangeSetKey revisionChangeSetKey)
        {
            return this.GetRevisionInfoPath(revisionChangeSetKey != null ? revisionChangeSetKey.FileSetId : 0L, revisionChangeSetKey != null ? revisionChangeSetKey.RevisionId : 0L, revisionChangeSetKey != null ? revisionChangeSetKey.PatchRevisionId : 0L);
        }

        private string GetRevisionInfoPath(long fileSetId, long revisionId, long patchRevisionId)
        {
            return Path.Combine(this.GetFileSetDirectory(fileSetId), this.GetRevisionInfoName(revisionId, patchRevisionId));
        }

        private string GetStagingDirectory(long fileSetId, long revisionId)
        {
            return Path.Combine(UpdateClientService.API.Services.FileSets.Constants.FileSetsRoot, "staging", string.Format("{0}", (object)fileSetId), string.Format("{0}", (object)revisionId));
        }

        private string GetRevisionName(long revisionId, long patchRevisionId)
        {
            return string.Format("{0}-{1}.revision", (object)revisionId, (object)patchRevisionId);
        }

        private string GetRevisionInfoName(long revisionId, long patchRevisionId)
        {
            return string.Format("{0}-{1}.revisioninfo", (object)revisionId, (object)patchRevisionId);
        }

        private string GetRevisionFileDirectory(long fileSetId)
        {
            return Path.Combine(this.GetFileSetDirectory(fileSetId), "File");
        }

        private string GetRevisionFilePath(long fileSetId, long fileId, long fileRevisionId)
        {
            return Path.Combine(this.GetRevisionFileDirectory(fileSetId), this.GetRevisionFileName(fileId, fileRevisionId));
        }

        private string GetFileInfoPath(long fileSetId, long fileId, long fileRevisionId)
        {
            return Path.Combine(this.GetRevisionFileDirectory(fileSetId), this.GetFileInfoName(fileId, fileRevisionId));
        }

        public string GetRevisionFileName(long fileId, long fileRevisionId)
        {
            return string.Format("{0}-{1}.file", (object)fileId, (object)fileRevisionId);
        }

        private string GetFileInfoName(long fileId, long fileRevisionId)
        {
            return string.Format("{0}-{1}.fileinfo", (object)fileId, (object)fileRevisionId);
        }

        private void CheckFilePath(long fileSetId, long fileId)
        {
            this.CreateDirectoryIfNeeded(Path.Combine(this.GetRevisionFileDirectory(fileSetId)));
        }

        private void CheckFileSetPath(long fileSetId)
        {
            this.CreateDirectoryIfNeeded(Path.Combine(this.GetFileSetDirectory(fileSetId)));
        }

        private void CreateDirectoryIfNeeded(string directoryPath)
        {
            if (Directory.Exists(directoryPath))
                return;
            Directory.CreateDirectory(directoryPath);
        }
    }
}
