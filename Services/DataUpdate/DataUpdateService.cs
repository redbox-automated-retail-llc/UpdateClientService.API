using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT;
using UpdateClientService.API.Services.IoT.Commands;
using UpdateClientService.API.Services.IoT.Commands.KioskFiles;
using UpdateClientService.API.Services.IoT.IoTCommand;

namespace UpdateClientService.API.Services.DataUpdate
{
    public class DataUpdateService : IDataUpdateService
    {
        private readonly ILogger<DataUpdateService> _logger;
        private readonly IIoTCommandClient _iotCommandClient;
        private string DefaultDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Redbox\\Data");

        public DataUpdateService(ILogger<DataUpdateService> logger, IIoTCommandClient iotCommandClient)
        {
            this._logger = logger;
            this._iotCommandClient = iotCommandClient;
        }

        public async Task<GetRecordChangesResponse> GetRecordChanges(DataUpdateRequest dataUpdateRequest)
        {
            DataUpdateService.RecordChangesData recordChangesData = new DataUpdateService.RecordChangesData()
            {
                DataUpdateRequest = dataUpdateRequest
            };
            try
            {
                while (!recordChangesData.AllPagesLoadedOrAborted)
                {
                    await this.SendDataUpdateRequest(recordChangesData);
                    this.GetDataUpdateResponse(recordChangesData);
                    await this.ProcessDataUpdateResponse(recordChangesData);
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, string.Format("Error in GetRecordChanges for Table  {0}.", (object)dataUpdateRequest?.TableName), nameof(GetRecordChanges), "/sln/src/UpdateClientService.API/Services/DataUpdate/DataUpdateService.cs");
                recordChangesData.GetRecordChangesResponse.StatusCode = HttpStatusCode.InternalServerError;
            }
            return recordChangesData.GetRecordChangesResponse;
        }

        private async Task ProcessDataUpdateResponse(
          DataUpdateService.RecordChangesData recordChangesData)
        {
            if (recordChangesData.AllPagesLoadedOrAborted)
                return;
            if (recordChangesData.CurrentDataUpdateResponse.IsFirstPage)
            {
                recordChangesData.DataUpdateResponse = recordChangesData.CurrentDataUpdateResponse;
                if (!recordChangesData.CurrentDataUpdateResponse.IsLastPage)
                    this._logger.LogInfoWithSource(string.Format("Response has {0} pages", (object)(int?)recordChangesData.CurrentDataUpdateResponse?.PageCount), nameof(ProcessDataUpdateResponse), "/sln/src/UpdateClientService.API/Services/DataUpdate/DataUpdateService.cs");
            }
            else
            {
                List<RecordResponse> recordResponses = recordChangesData.CurrentDataUpdateResponse.RecordResponses;
                if ((recordResponses != null ? (recordResponses.Any<RecordResponse>() ? 1 : 0) : 0) != 0)
                    recordChangesData.DataUpdateResponse.RecordResponses.AddRange((IEnumerable<RecordResponse>)recordChangesData.CurrentDataUpdateResponse.RecordResponses);
            }
            if (recordChangesData.CurrentDataUpdateResponse.IsLastPage)
            {
                recordChangesData.DataUpdateResponse.PageCount = new int?();
                recordChangesData.DataUpdateResponse.PageNumber = new int?();
                if (await this.WriteRecordUpatesToFile(recordChangesData))
                    recordChangesData.GetRecordChangesResponse.StatusCode = HttpStatusCode.OK;
                else
                    recordChangesData.GetRecordChangesResponse.StatusCode = HttpStatusCode.InternalServerError;
                recordChangesData.AllPagesLoadedOrAborted = true;
            }
            else
            {
                DataUpdateRequest dataUpdateRequest = recordChangesData.DataUpdateRequest;
                int? pageNumber = recordChangesData.CurrentDataUpdateResponse.PageNumber;
                int? nullable = pageNumber.HasValue ? new int?(pageNumber.GetValueOrDefault() + 1) : new int?();
                dataUpdateRequest.PageNumber = nullable;
            }
        }

        private void GetDataUpdateResponse(
          DataUpdateService.RecordChangesData recordChangesData)
        {
            if (recordChangesData.AllPagesLoadedOrAborted)
                return;
            IoTCommandResponse<ObjectResult> tcommandResponse = recordChangesData.IoTCommandResponse;
            if ((tcommandResponse != null ? (tcommandResponse.StatusCode == 200 ? 1 : 0) : 0) != 0)
            {
                try
                {
                    recordChangesData.CurrentDataUpdateResponse = JsonConvert.DeserializeObject<DataUpdateResponse>(recordChangesData.IoTCommandResponse?.Payload?.Value?.ToString());
                    if (recordChangesData.CurrentDataUpdateResponse != null)
                        return;
                    recordChangesData.GetRecordChangesResponse.StatusCode = HttpStatusCode.InternalServerError;
                    this._logger.LogErrorWithSource(string.Format("Unable to deserialize Iot command payload as {0} for RequestId {1}", (object)"DataUpdateResponse", (object)recordChangesData?.DataUpdateRequest?.RequestId), nameof(GetDataUpdateResponse), "/sln/src/UpdateClientService.API/Services/DataUpdate/DataUpdateService.cs");
                    recordChangesData.AllPagesLoadedOrAborted = true;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, string.Format("Exception while deserializing {0} for RequestId {1}", (object)"DataUpdateResponse", (object)recordChangesData?.DataUpdateRequest?.RequestId), nameof(GetDataUpdateResponse), "/sln/src/UpdateClientService.API/Services/DataUpdate/DataUpdateService.cs");
                }
            }
            else
            {
                recordChangesData.GetRecordChangesResponse.StatusCode = HttpStatusCode.InternalServerError;
                this._logger.LogErrorWithSource(string.Format("Iot command {0} returned statuscode {1}", (object)CommandEnum.GetRecordChanges, (object)recordChangesData.IoTCommandResponse?.StatusCode), nameof(GetDataUpdateResponse), "/sln/src/UpdateClientService.API/Services/DataUpdate/DataUpdateService.cs");
                recordChangesData.AllPagesLoadedOrAborted = true;
            }
        }

        private async Task SendDataUpdateRequest(
          DataUpdateService.RecordChangesData recordChangesData)
        {
            DataUpdateService.RecordChangesData recordChangesData1 = recordChangesData;
            IIoTCommandClient iotCommandClient = this._iotCommandClient;
            recordChangesData1.IoTCommandResponse = await iotCommandClient.PerformIoTCommand<ObjectResult>(new IoTCommandModel()
            {
                Version = 1,
                RequestId = Guid.NewGuid().ToString(),
                Command = CommandEnum.GetRecordChanges,
                Payload = (object)recordChangesData.DataUpdateRequest,
                QualityOfServiceLevel = QualityOfServiceLevel.AtLeastOnce,
                LogResponse = new bool?(false),
                LogRequest = new bool?(false)
            }, new PerformIoTCommandParameters()
            {
                IoTTopic = "$aws/rules/kioskrestcall",
                WaitForResponse = true
            });
            recordChangesData1 = (DataUpdateService.RecordChangesData)null;
        }

        private async Task<bool> WriteRecordUpatesToFile(
          DataUpdateService.RecordChangesData recordChangesData)
        {
            bool result = false;
            string json = (string)null;
            string filePath = this.GetDataUpdateResponseFileName(recordChangesData);
            await Task.Run((Func<Task>)(async () =>
            {
                try
                {
                    bool flag = !string.IsNullOrEmpty(filePath);
                    if (flag)
                        flag = await this.CreateFilePathIfNeeded(filePath);
                    if (!flag || recordChangesData?.DataUpdateResponse == null)
                        return;
                    json = recordChangesData.DataUpdateResponse.ToJsonIndented();
                    File.WriteAllText(filePath, json);
                    recordChangesData.GetRecordChangesResponse.DataUpdateResponseFileName = filePath;
                    result = true;
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, string.Format("Error saving configiration file {0}.  json length: {1}", (object)filePath, (object)json.Length), nameof(WriteRecordUpatesToFile), "/sln/src/UpdateClientService.API/Services/DataUpdate/DataUpdateService.cs");
                }
            }));
            return result;
        }

        private async Task<bool> CreateFilePathIfNeeded(string filePath)
        {
            bool result = true;
            await Task.Run((Action)(() =>
            {
                try
                {
                    string directoryName = Path.GetDirectoryName(filePath);
                    if (Directory.Exists(directoryName) || Directory.CreateDirectory(directoryName).Exists)
                        return;
                    result = false;
                    this._logger.LogErrorWithSource("Unable to create directory " + directoryName, nameof(CreateFilePathIfNeeded), "/sln/src/UpdateClientService.API/Services/DataUpdate/DataUpdateService.cs");
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Exception while checking/creating directory for " + filePath, nameof(CreateFilePathIfNeeded), "/sln/src/UpdateClientService.API/Services/DataUpdate/DataUpdateService.cs");
                }
            }));
            return result;
        }

        private string GetDataUpdateResponseFileName(
          DataUpdateService.RecordChangesData recordChangesData)
        {
            if (recordChangesData?.DataUpdateRequest == null)
            {
                this._logger.LogErrorWithSource("DataUpdateRequest must have a value.", nameof(GetDataUpdateResponseFileName), "/sln/src/UpdateClientService.API/Services/DataUpdate/DataUpdateService.cs");
                return (string)null;
            }
            string path2 = string.Format("{0}-update-{1}.json", (object)recordChangesData.DataUpdateRequest?.TableName, (object)recordChangesData.DataUpdateRequest?.RequestId);
            string path1 = (string)null;
            FileTypeEnum? fileType = recordChangesData.DataUpdateRequest.FileType;
            if (fileType.HasValue)
            {
                Dictionary<FileTypeEnum, string> typeMappings = FilePaths.TypeMappings;
                fileType = recordChangesData.DataUpdateRequest.FileType;
                int key = (int)fileType.Value;
                ref string local = ref path1;
                if (typeMappings.TryGetValue((FileTypeEnum)key, out local))
                    goto label_5;
            }
            path1 = this.DefaultDataFolder;
        label_5:
            path1 = Path.Combine(path1, recordChangesData.DataUpdateRequest.TableName.ToString());
            return Path.Combine(path1, path2);
        }

        private class RecordChangesData
        {
            public GetRecordChangesResponse GetRecordChangesResponse { get; set; } = new GetRecordChangesResponse();

            public DataUpdateResponse DataUpdateResponse { get; set; } = new DataUpdateResponse();

            public DataUpdateResponse CurrentDataUpdateResponse { get; set; }

            public IoTCommandResponse<ObjectResult> IoTCommandResponse { get; set; }

            public DataUpdateRequest DataUpdateRequest { get; set; }

            public bool AllPagesLoadedOrAborted { get; set; }
        }
    }
}
