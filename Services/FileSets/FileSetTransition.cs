using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UpdateClientService.API.Services.FileCache;
using UpdateClientService.API.Services.Utilities;

namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetTransition : IFileSetTransition
    {
        private static bool _rebootRequired;
        private const string FileSetChangeSetPath = "staging";
        private const string FileSetJsonFileName = "fileset.json";
        private IFileCacheService _fileCacheService;
        private IHttpService _httpService;
        private IWindowsServiceFunctions _winServiceFunctions;
        private readonly ICommandLineService _commandLineService;
        private AppSettings _appSettings;
        private ILogger<FileSetTransition> _logger;

        public FileSetTransition(
          IFileCacheService fileCacheService,
          IHttpService httpService,
          IWindowsServiceFunctions winServiceFunctions,
          ICommandLineService cmd,
          IOptions<AppSettings> appSettings,
          ILogger<FileSetTransition> logger)
        {
            this._fileCacheService = fileCacheService;
            this._httpService = httpService;
            this._winServiceFunctions = winServiceFunctions;
            this._commandLineService = cmd;
            this._appSettings = appSettings.Value;
            this._logger = logger;
        }

        public void MarkRebootRequired() => FileSetTransition._rebootRequired = true;

        public void ClearRebootRequired() => FileSetTransition._rebootRequired = false;

        public bool RebootRequired => FileSetTransition._rebootRequired;

        public bool Stage(ClientFileSetRevision clientFileSetRevision)
        {
            bool result = true;
            try
            {
                string fileSetRevisionPath = this.GetStagingFileSetRevisionPath(clientFileSetRevision);
                if (!Directory.Exists(fileSetRevisionPath))
                    Directory.CreateDirectory(fileSetRevisionPath);
                List<FileData> fileData = this.GetFileData(clientFileSetRevision);
                fileData.ForEach((Action<FileData>)(eachFileData =>
                {
                    if (this._fileCacheService.DoesFileExist(clientFileSetRevision.FileSetId, eachFileData.FileId, eachFileData.FileRevisionId))
                        return;
                    this._logger.LogError(string.Format("Missing file for FileId {0} Revision {1}", (object)eachFileData.FileId, (object)eachFileData.FileRevisionId), Array.Empty<object>());
                    result = false;
                }));
                if (!result)
                    return result;
                fileData.ForEach((Action<FileData>)(eachFileData =>
                {
                    if (this._fileCacheService.CopyFileToPath(clientFileSetRevision.FileSetId, eachFileData.FileId, eachFileData.FileRevisionId, eachFileData.StagePath))
                        return;
                    this._logger.LogError(string.Format("File for FileId {0} Revision {1} couldn't be copied to it's staging directory", (object)eachFileData.FileId, (object)eachFileData.FileRevisionId), Array.Empty<object>());
                    result = false;
                }));
                if (!result)
                    return result;
                fileData.ForEach((Action<FileData>)(eachFileData =>
                {
                    if (this._fileCacheService.IsFileHashValid(eachFileData.StagePath, eachFileData.Hash))
                        return;
                    this._logger.LogError(string.Format("Hash check failed for FileId {0} Revision {1}", (object)eachFileData.FileId, (object)eachFileData.FileRevisionId), Array.Empty<object>());
                    result = false;
                }));
            }
            catch (Exception ex)
            {
                ILogger<FileSetTransition> logger = this._logger;
                Exception exception = ex;
                ClientFileSetRevision revisionChangeSetKey = clientFileSetRevision;
                string str = "Exception while staging files for revision with " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                object[] objArray = Array.Empty<object>();
                this._logger.LogError(exception, str, objArray);
            }
            return result;
        }

        public bool CheckMeetsDependency(
          ClientFileSetRevision clientFileSetRevision,
          Dictionary<long, FileSetDependencyState> dependencyStates)
        {
            bool flag = false;
            try
            {
                bool? nullable;
                if (clientFileSetRevision == null)
                {
                    nullable = new bool?();
                }
                else
                {
                    IEnumerable<ClientFileSetRevisionDependency> dependencies = clientFileSetRevision.Dependencies;
                    nullable = dependencies != null ? new bool?(dependencies.Any<ClientFileSetRevisionDependency>()) : new bool?();
                }
                if (!nullable.GetValueOrDefault())
                    return true;
                flag = true;
                foreach (ClientFileSetRevisionDependency dependency in clientFileSetRevision.Dependencies)
                {
                    if (!dependencyStates.ContainsKey(dependency.DependsOnFileSetId))
                    {
                        this._logger.LogWarningWithSource(string.Format("Missing dependency FileSet with FileSetId {0} for revision {1} Revision {2}", (object)dependency.DependsOnFileSetId, (object)clientFileSetRevision.IdentifyingText(), (object)clientFileSetRevision.RevisionId), nameof(CheckMeetsDependency), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                        flag = false;
                    }
                    else
                    {
                        FileSetDependencyState dependencyState = dependencyStates[dependency.DependsOnFileSetId];
                        if (!dependencyState.IsDependencyMet(dependency.DependencyType, dependency.MinimumVersion, dependency.MaximumVersion))
                        {
                            this._logger.LogWarningWithSource("Missing dependency version for " + clientFileSetRevision.IdentifyingText() + ". Expected version " + FileSetTransition.ExpectedDependencyVersionInfo(dependency, dependencyState) + ". Current version: '" + (dependencyState.InProgressRevisionId > 0L ? dependencyState.InProgressVersion : dependencyState.ActiveVersion) + "'", nameof(CheckMeetsDependency), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                            flag = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Exception while checking dependencies for revision " + (clientFileSetRevision != null ? clientFileSetRevision.IdentifyingText() : (string)null), Array.Empty<object>());
            }
            return flag;
        }

        private static string ExpectedDependencyVersionInfo(
          ClientFileSetRevisionDependency clientFileSetRevisionDependency,
          FileSetDependencyState fileSetDependencyState)
        {
            return new
            {
                FileSetId = fileSetDependencyState.FileSetId,
                DependencyType = clientFileSetRevisionDependency.DependencyType.ToString(),
                MinimumVersion = clientFileSetRevisionDependency.MinimumVersion,
                MaximumVersion = clientFileSetRevisionDependency.MaximumVersion
            }.ToJson();
        }

        private async Task CallUpdateClientInstaller(ClientFileSetRevision clientFileSetRevision)
        {
            this._logger.LogInfoWithSource("Calling UpdateClientInstaller for revision with " + clientFileSetRevision.ToJsonIndented(), nameof(CallUpdateClientInstaller), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
            APIResponse<object> apiResponse = await this._httpService.SendRequestAsync<object>(this._appSettings.UpdateClientInstallerUrl, "api/activation?waitForResponse=false", (object)clientFileSetRevision, HttpMethod.Post, callerMethod: nameof(CallUpdateClientInstaller), callerLocation: "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs", logRequest: false);
            if (apiResponse.IsSuccessStatusCode)
                return;
            this._logger.LogErrorWithSource("Error while calling UpdateClientInstaller.Error-> " + apiResponse.Errors.ToJson(), nameof(CallUpdateClientInstaller), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
        }

        public bool BeforeActivate(ClientFileSetRevision clientFileSetRevision)
        {
            bool flag = true;
            try
            {
                FileSetJsonModel fileSetJsonModel = this.GetFileSetJsonModel(clientFileSetRevision, true);
                if (fileSetJsonModel == null)
                {
                    this._logger.LogWarningWithSource("FileSetJson was null skipping BeforeActivate.", nameof(BeforeActivate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                    return flag;
                }
                if (fileSetJsonModel?.WindowsServiceName == "updateclient$service")
                {
                    Task.Run((Func<Task>)(async () => await this.CallUpdateClientInstaller(clientFileSetRevision)));
                    return false;
                }
                if (!string.IsNullOrWhiteSpace(fileSetJsonModel?.WindowsServiceName) && this._winServiceFunctions.ServiceExists(fileSetJsonModel.WindowsServiceName))
                    this._winServiceFunctions.StopService(fileSetJsonModel.WindowsServiceName);
                FileData fileData = this.GetFileData(clientFileSetRevision).Where<FileData>((Func<FileData, bool>)(f => string.Equals(Path.GetFileName(f.FileDestination), "beforeactivate.ps1", StringComparison.OrdinalIgnoreCase))).FirstOrDefault<FileData>();
                if (fileData != null)
                {
                    flag = this.RunScript(clientFileSetRevision, fileData.StagePath);
                    if (!flag)
                    {
                        ILogger<FileSetTransition> logger = this._logger;
                        ClientFileSetRevision revisionChangeSetKey = clientFileSetRevision;
                        string str = "Error running BeforeActivate script for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                        this._logger.LogErrorWithSource(str, nameof(BeforeActivate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                    }
                }
            }
            catch (Exception ex)
            {
                ILogger<FileSetTransition> logger = this._logger;
                Exception exception = ex;
                ClientFileSetRevision revisionChangeSetKey = clientFileSetRevision;
                string str = "Exception while processing BeforeActivate for " + (revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(BeforeActivate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                flag = false;
            }
            return flag;
        }

        private FileSetJsonModel GetFileSetJsonModel(
          ClientFileSetRevision clientFileSetRevision,
          bool beforeActivations)
        {
            FileSetJsonModel fileSetJsonModel = (FileSetJsonModel)null;
            FileData fileData = this.GetFileData(clientFileSetRevision).FirstOrDefault<FileData>((Func<FileData, bool>)(x => Path.GetFileName(x.FileDestination).ToLower() == "fileset.json"));
            if (fileData == null)
            {
                this._logger.LogWarningWithSource("fileset.json does not exist for revision: " + clientFileSetRevision.IdentifyingText(), nameof(GetFileSetJsonModel), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                return (FileSetJsonModel)null;
            }
            string path = beforeActivations ? fileData.StagePath : fileData.FileDestination;
            try
            {
                fileSetJsonModel = File.ReadAllText(path).ToObject<FileSetJsonModel>();
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while getting fileset.json from " + path + ".", nameof(GetFileSetJsonModel), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
            }
            return fileSetJsonModel;
        }

        public bool AfterActivate(ClientFileSetRevision clientFileSetRevision)
        {
            bool flag = true;
            try
            {
                FileSetJsonModel fileSetJsonModel = this.GetFileSetJsonModel(clientFileSetRevision, false);
                if (fileSetJsonModel == null)
                {
                    this._logger.LogWarningWithSource("FileSetJson was null skipping AfterActivate.", nameof(AfterActivate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                    return flag;
                }
                if (!string.IsNullOrWhiteSpace(fileSetJsonModel?.WindowsServiceName))
                    flag = this.InstallAndStartWindowsService(fileSetJsonModel);
                FileData fileData = this.GetFileData(clientFileSetRevision).Where<FileData>((Func<FileData, bool>)(f => string.Equals(Path.GetFileName(f.FileDestination), "afteractivate.ps1", StringComparison.OrdinalIgnoreCase))).FirstOrDefault<FileData>();
                if (fileData != null)
                {
                    if (!this.RunScript(clientFileSetRevision, fileData.FileDestination))
                    {
                        this._logger.LogErrorWithSource("Error while running afteractivate script for " + (clientFileSetRevision != null ? clientFileSetRevision.IdentifyingText() : (string)null), nameof(AfterActivate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                        flag = false;
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while running afteractivate script for " + (clientFileSetRevision != null ? clientFileSetRevision.IdentifyingText() : (string)null), nameof(AfterActivate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                flag = false;
            }
            return flag;
        }

        private bool InstallAndStartWindowsService(FileSetJsonModel fileSetJson)
        {
            bool flag = true;
            if (!this._winServiceFunctions.ServiceExists(fileSetJson.WindowsServiceName))
            {
                if (!string.IsNullOrWhiteSpace(fileSetJson.WindowsServiceStartFile))
                {
                    string displayName = string.IsNullOrWhiteSpace(fileSetJson.WindowsServiceDisplayName) ? fileSetJson.WindowsServiceName : fileSetJson.WindowsServiceDisplayName;
                    string binPath = Path.Combine(fileSetJson.InstallPath, fileSetJson.WindowsServiceStartFile);
                    this._winServiceFunctions.InstallService(fileSetJson.WindowsServiceName, displayName, binPath);
                }
                else
                {
                    this._logger.LogErrorWithSource("windowsServiceStartPath in fileSetJson is null for windowsServiceName " + fileSetJson.WindowsServiceName, nameof(InstallAndStartWindowsService), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                    flag = false;
                }
            }
            this._winServiceFunctions.StartService(fileSetJson.WindowsServiceName);
            return flag;
        }

        public bool Activate(ClientFileSetRevision clientFileSetRevision)
        {
            bool flag;
            try
            {
                List<FileData> fileData1 = this.GetFileData(clientFileSetRevision);
                flag = this.StagedFilesExist(clientFileSetRevision, fileData1);
                if (!flag)
                    return flag;
                Action<string, string> action = (Action<string, string>)((sourceFilePath, destinationfilePath) =>
                {
                    if (File.Exists(destinationfilePath))
                        File.Delete(destinationfilePath);
                    File.Move(sourceFilePath, destinationfilePath);
                });
                foreach (FileData fileData2 in fileData1)
                {
                    flag = this.CreateFileDestinationDirectory(fileData2);
                    if (!flag)
                        return flag;
                    try
                    {
                        action(fileData2.StagePath, fileData2.FileDestination);
                    }
                    catch (Exception ex1)
                    {
                        try
                        {
                            this._logger.LogInfoWithSource(fileData2.FileDestination + " is locked by anther process attempting to retry.", nameof(Activate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                            this._logger.LogErrorWithSource(ex1, "Error from activate copy - retrying.", nameof(Activate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                            Task.Delay(100).Wait();
                            action(fileData2.StagePath, fileData2.FileDestination);
                        }
                        catch (Exception ex2)
                        {
                            this._logger.LogInfoWithSource(fileData2.FileDestination + " is locked by anther process doing a deferred move.", nameof(Activate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                            this._logger.LogErrorWithSource(ex2, "Error from activate copy.", nameof(Activate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                            string tempFileName = Path.GetTempFileName();
                            action(fileData2.StagePath, tempFileName);
                            FileSetTransition.MoveLockedFileSystemEntry(tempFileName, fileData2.FileDestination);
                            this.MarkRebootRequired();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while activing revision with " + (clientFileSetRevision != null ? clientFileSetRevision.IdentifyingText() : (string)null), nameof(Activate), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                flag = false;
            }
            return flag;
        }

        private bool StagedFilesExist(
          ClientFileSetRevision clientFileSetRevision,
          List<FileData> fileDataList)
        {
            bool result = true;
            fileDataList?.ForEach((Action<FileData>)(eachFileData =>
            {
                if (File.Exists(eachFileData.StagePath))
                    return;
                ILogger<FileSetTransition> logger = this._logger;
                string stagePath = eachFileData.StagePath;
                long fileId = eachFileData.FileId;
                ClientFileSetRevision revisionChangeSetKey = clientFileSetRevision;
                string str1 = revisionChangeSetKey != null ? revisionChangeSetKey.IdentifyingText() : (string)null;
                string str2 = string.Format("Missing file {0} for FileId {1} and revision with {2}", (object)stagePath, (object)fileId, (object)str1);
                this._logger.LogErrorWithSource(str2, nameof(StagedFilesExist), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                result = false;
            }));
            return result;
        }

        private bool CreateFileDestinationDirectory(FileData fileData)
        {
            bool destinationDirectory = true;
            try
            {
                string directoryName = Path.GetDirectoryName(fileData.FileDestination);
                if (!Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while creating directory " + fileData?.FileDestination, nameof(CreateFileDestinationDirectory), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                destinationDirectory = false;
            }
            return destinationDirectory;
        }

        private static void MoveLockedFileSystemEntry(string source, string destination)
        {
            FileSetTransition.MoveFileFlags dwFlags = FileSetTransition.MoveFileFlags.MOVEFILE_DELAY_UNTIL_REBOOT;
            if (!Directory.Exists(source) && !Directory.Exists(destination))
                dwFlags |= FileSetTransition.MoveFileFlags.MOVEFILE_REPLACE_EXISTING;
            FileSetTransition.MoveFileEx(source, destination, dwFlags);
        }

        [DllImport("kernel32.dll")]
        private static extern bool MoveFileEx(
          string lpExistingFileName,
          string lpNewFileName,
          FileSetTransition.MoveFileFlags dwFlags);

        private List<FileData> GetFileData(ClientFileSetRevision clientFileSetRevision)
        {
            List<FileData> fileData1 = new List<FileData>();
            string fileSetRevisionPath = this.GetStagingFileSetRevisionPath(clientFileSetRevision);
            foreach (ClientFileSetFile file in clientFileSetRevision.Files)
            {
                FileData fileData2 = new FileData()
                {
                    FileId = file.FileId,
                    FileRevisionId = file.FileRevisionId,
                    Size = file.ContentSize,
                    Hash = file.ContentHash,
                    StagePath = Path.Combine(fileSetRevisionPath, this._fileCacheService.GetRevisionFileName(file.FileId, file.FileRevisionId)),
                    FileDestination = file.FileDestination
                };
                fileData1.Add(fileData2);
            }
            return fileData1;
        }

        private bool RunScript(ClientFileSetRevision clientFileSetRevision, string scriptPath)
        {
            try
            {
                if (string.IsNullOrEmpty(scriptPath))
                {
                    this._logger.LogWarningWithSource("Script was empty for " + clientFileSetRevision.IdentifyingText() + ". Skipping...", nameof(RunScript), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                    return true;
                }
                if (!(Path.GetExtension(scriptPath) == ".file"))
                    return this._commandLineService.TryExecutePowerShellScriptFromFile(scriptPath);
                string str = Path.GetTempFileName().Replace(".tmp", ".ps1");
                File.Copy(scriptPath, str);
                bool flag = this._commandLineService.TryExecutePowerShellScriptFromFile(str);
                File.Delete(str);
                return flag;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while running script " + scriptPath, nameof(RunScript), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetTransition.cs");
                return false;
            }
        }

        private string GetStagingDirectory() => Path.Combine(Constants.FileSetsRoot, "staging");

        private string GetStagingFileSetPath(ClientFileSetRevision clientFileSetRevision)
        {
            return Path.Combine(this.GetStagingDirectory(), clientFileSetRevision.FileSetId.ToString());
        }

        private string GetStagingFileSetRevisionPath(ClientFileSetRevision clientFileSetRevision)
        {
            return Path.Combine(this.GetStagingFileSetPath(clientFileSetRevision), clientFileSetRevision.RevisionId.ToString());
        }

        [Flags]
        private enum MoveFileFlags
        {
            MOVEFILE_REPLACE_EXISTING = 1,
            MOVEFILE_COPY_ALLOWED = 2,
            MOVEFILE_DELAY_UNTIL_REBOOT = 4,
            MOVEFILE_WRITE_THROUGH = 8,
        }
    }
}
