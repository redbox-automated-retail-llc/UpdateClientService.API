using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UpdateClientService.API.App;
using UpdateClientService.API.Services.Files;
using UpdateClientService.API.Services.IoT.IoTCommand;

namespace UpdateClientService.API.Services.IoT.Commands.KioskFiles
{
    public class KioskFilesService : IKioskFilesService
    {
        private readonly ILogger<KioskFilesService> _logger;
        private readonly IFileService _fileService;
        private readonly IIoTProcessStatusService _ioTProcessStatusService;

        public KioskFilesService(
          ILogger<KioskFilesService> logger,
          IFileService fileService,
          IIoTProcessStatusService ioTProcessStatusService,
          IIoTCommandClient iotCommandClient)
        {
            this._logger = logger;
            this._fileService = fileService;
            this._ioTProcessStatusService = ioTProcessStatusService;
        }

        public async Task<MqttResponse<Dictionary<string, string>>> PeekRequestedFilesAsync(
          KioskFilePeekRequest logRequestModel)
        {
            MqttResponse<Dictionary<string, string>> mqttResponse1 = new MqttResponse<Dictionary<string, string>>()
            {
                Data = new Dictionary<string, string>()
            };
            List<FileInfo> fileInfoList = new List<FileInfo>();
            try
            {
                List<FileInfo> files;
                if (!string.IsNullOrWhiteSpace(logRequestModel.FileQuery?.Path))
                {
                    MqttResponse<Dictionary<string, string>> mqttResponse2 = mqttResponse1;
                    (files, mqttResponse2.Error) = await this.GetFilesByQuery(logRequestModel);
                    mqttResponse2 = (MqttResponse<Dictionary<string, string>>)null;
                }
                else
                {
                    MqttResponse<Dictionary<string, string>> mqttResponse = mqttResponse1;
                    (List<FileInfo> files, string error) filesByFileType = this.GetFilesByFileType(logRequestModel);
                    files = filesByFileType.files;
                    string error;
                    string str = error = filesByFileType.error;
                    mqttResponse.Error = error;
                }
                if (files != null && (files.Count) > 0)
                    KioskFilesService.FilterFilesByDate(ref files, logRequestModel.Date, logRequestModel.EndDate);
                files?.ForEach((Action<FileInfo>)(file =>
                {
                    string str = file.LastWriteTime.ToString();
                    if (!string.IsNullOrWhiteSpace(logRequestModel.FileQuery?.Path))
                    {
                        FileQuery fileQuery = logRequestModel.FileQuery;
                        int num;
                        if (fileQuery == null)
                        {
                            num = 0;
                        }
                        else
                        {
                            bool recurseSubdirectories = fileQuery.SearchOptions == SearchOption.AllDirectories;
                            bool flag = true;
                            num = recurseSubdirectories == flag ? 1 : 0;
                        }
                        if (num != 0)
                        {
                            mqttResponse1.Data.Add(PathHelpers.GetRelativePath(logRequestModel.FileQuery.Path, file.FullName), str);
                            return;
                        }
                    }
                    mqttResponse1.Data.Add(file.Name, str);
                }));
                if (!mqttResponse1.Data.Any<KeyValuePair<string, string>>())
                    mqttResponse1.Error = "There were no files found for given request. Request -> " + logRequestModel.ToJson() + ", Additional Errors -> " + mqttResponse1.Error;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while gettings FileInfos", nameof(PeekRequestedFilesAsync), "/sln/src/UpdateClientService.API/Services/IoT/Commands/KioskFiles/KioskFilesService.cs");
                mqttResponse1.Error = ex.Message + ". Logged full exception in UpdateClientService logs, take a look at it if you want to see the full exception details.";
                return mqttResponse1;
            }
            this._logger.LogInfoWithSource(string.Format("Returning ${0} files.", (object)mqttResponse1.Data.Count), nameof(PeekRequestedFilesAsync), "/sln/src/UpdateClientService.API/Services/IoT/Commands/KioskFiles/KioskFilesService.cs");
            return mqttResponse1;
        }

        public async Task<MqttResponse<string>> UploadFilesAsync(KioskUploadFileRequest kioskFileRequest)
        {
            if (this._ioTProcessStatusService.UploadFilesStatus == IoTProcessStatusEnum.InProgress)
            {
                this._logger.LogWarningWithSource("There was another upload files request while uploading files for another request.", nameof(UploadFilesAsync), "/sln/src/UpdateClientService.API/Services/IoT/Commands/KioskFiles/KioskFilesService.cs");
                return new MqttResponse<string>()
                {
                    Error = "UpdateClientService: Already uploading files for another request, please try again in 1 or 2 minutes."
                };
            }
            if (kioskFileRequest.FileNames.Count > 30)
                return new MqttResponse<string>()
                {
                    Error = "UpdateClientService: Can't upload more than 30 files at one time. Contact Kiosk Devs for questions."
                };
            MqttResponse<string> mqttResponse = new MqttResponse<string>()
            {
                Data = kioskFileRequest.S3PreSignedUrl
            };
            string path;
            if (!string.IsNullOrWhiteSpace(kioskFileRequest?.BasePath))
            {
                path = kioskFileRequest.BasePath;
            }
            else
            {
                string error;
                if (!this.TryGetRequestedLogPath(kioskFileRequest.FileType, out path, out error))
                {
                    this._logger.LogErrorWithSource("An error occurred while trying to upload requested file to S3. Request Model -> " + kioskFileRequest.ToJson() + ". Error -> " + error, nameof(UploadFilesAsync), "/sln/src/UpdateClientService.API/Services/IoT/Commands/KioskFiles/KioskFilesService.cs");
                    mqttResponse.Error = "An error occurred while trying to upload requested file to S3. Take a look at UCS logs for full exception details. Error -> " + error;
                    this._ioTProcessStatusService.UploadFilesStatus = IoTProcessStatusEnum.Errored;
                    return mqttResponse;
                }
            }
            try
            {
                this._ioTProcessStatusService.UploadFilesStatus = IoTProcessStatusEnum.InProgress;
                if (kioskFileRequest.FileNames.Count > 1 || kioskFileRequest.ZipFiles)
                {
                    List<FileInfo> fileToUpload = new List<FileInfo>();
                    foreach (string fileName1 in kioskFileRequest.FileNames)
                    {
                        string fileName2 = Path.Combine(path, fileName1);
                        fileToUpload.Add(new FileInfo(fileName2));
                    }
                    await this._fileService.ZipAndUploadToS3(fileToUpload, kioskFileRequest.S3PreSignedUrl);
                }
                else
                {
                    string filePath = Path.Combine(path, kioskFileRequest.FileNames[0]);
                    await this._fileService.UploadFileToS3(kioskFileRequest.S3PreSignedUrl, filePath);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "An error occurred while trying to upload requested file to S3. Request Model -> " + kioskFileRequest.ToJson(), nameof(UploadFilesAsync), "/sln/src/UpdateClientService.API/Services/IoT/Commands/KioskFiles/KioskFilesService.cs");
                mqttResponse.Error = "An error occurred while trying to upload requested file to S3. Take a look at UCS logs for full exception details. Error -> " + ex.Message;
                this._ioTProcessStatusService.UploadFilesStatus = IoTProcessStatusEnum.Errored;
            }
            finally
            {
                this._ioTProcessStatusService.UploadFilesStatus = this._ioTProcessStatusService.UploadFilesStatus == IoTProcessStatusEnum.Errored ? IoTProcessStatusEnum.Errored : IoTProcessStatusEnum.Finished;
            }
            this._logger.LogInfoWithSource("Returning mqttresponse: " + mqttResponse.ToJson(), nameof(UploadFilesAsync), "/sln/src/UpdateClientService.API/Services/IoT/Commands/KioskFiles/KioskFilesService.cs");
            return mqttResponse;
        }

        private (List<FileInfo> files, string error) GetFilesByFileType(
          KioskFilePeekRequest logRequestModel)
        {
            List<FileInfo> fileInfoList = new List<FileInfo>();
            string error;
            try
            {
                string path;
                if (!this.TryGetRequestedLogPath(logRequestModel.FileType, out path, out error))
                    return ((List<FileInfo>)null, "Couldn't get path of " + (logRequestModel.FileType ?? "null") + ". Error -> " + error);
                FileInfo[] files = new DirectoryInfo(path).GetFiles();
                fileInfoList = (files != null ? ((IEnumerable<FileInfo>)files).ToList<FileInfo>() : (List<FileInfo>)null) ?? new List<FileInfo>();
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while getting FileInfos", nameof(GetFilesByFileType), "/sln/src/UpdateClientService.API/Services/IoT/Commands/KioskFiles/KioskFilesService.cs");
                error = ex.Message;
            }
            return (fileInfoList, error);
        }

        private async Task<(List<FileInfo> files, string error)> GetFilesByQuery(
          KioskFilePeekRequest logRequestModel,
          TimeSpan? timeout = null)
        {
            return await Task.Run((Func<(List<FileInfo>, string)>)(() =>
            {
                List<FileInfo> fileInfoList = new List<FileInfo>();
                string str = null;
                try
                {
                    FileQuery fileQuery = logRequestModel.FileQuery;
                    fileInfoList = (new DirectoryInfo(fileQuery.Path).GetFiles(
              !string.IsNullOrWhiteSpace(fileQuery.SearchPattern) ? fileQuery.SearchPattern : "*",
              fileQuery.SearchOptions
          )).ToList<FileInfo>() ?? new List<FileInfo>();
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Exception while getting FileInfos", nameof(GetFilesByQuery), "/sln/src/UpdateClientService.API/Services/IoT/Commands/KioskFiles/KioskFilesService.cs");
                    str = ex.Message;
                }
                return (fileInfoList, str);
            }), (timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource(TimeSpan.FromSeconds(90.0))).Token);
        }

        private static void FilterFilesByDate(
          ref List<FileInfo> files,
          DateTime? date,
          DateTime? endDate)
        {
            if (files == null || files.Count == 0)
                return;
            if (!date.Equals((object)null) && !endDate.Equals((object)null))
                files = files.Where<FileInfo>((Func<FileInfo, bool>)(x =>
                {
                    DateTime lastWriteTime = x.LastWriteTime;
                    DateTime? nullable = date;
                    return (nullable.HasValue ? (lastWriteTime >= nullable.GetValueOrDefault() ? 1 : 0) : 0) != 0 && x.LastWriteTime <= endDate.Value.AddDays(1.0);
                })).ToList<FileInfo>();
            else if (!date.Equals((object)null))
                files = files.Where<FileInfo>((Func<FileInfo, bool>)(x =>
                {
                    DateTime dateTime = x.LastWriteTime;
                    string shortDateString = dateTime.ToShortDateString();
                    ref DateTime? local = ref date;
                    string str;
                    if (!local.HasValue)
                    {
                        str = (string)null;
                    }
                    else
                    {
                        dateTime = local.GetValueOrDefault();
                        str = dateTime.ToShortDateString();
                    }
                    return shortDateString == str;
                })).ToList<FileInfo>();
            else
                files = files.ToList<FileInfo>();
        }

        private bool TryGetRequestedLogPath(string logType, out string path, out string error)
        {
            path = (string)null;
            error = (string)null;
            FileTypeEnum result;
            if (Enum.TryParse<FileTypeEnum>(logType, out result) && FilePaths.TypeMappings.TryGetValue(result, out path) && !string.IsNullOrWhiteSpace(path))
                return true;
            error = (logType ?? "null") + " was not found in FilePaths";
            this._logger.LogErrorWithSource(error, nameof(TryGetRequestedLogPath), "/sln/src/UpdateClientService.API/Services/IoT/Commands/KioskFiles/KioskFilesService.cs");
            return false;
        }
    }
}
