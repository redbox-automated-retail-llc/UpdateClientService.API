using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UpdateClientService.API.Services.FileSets;
using UpdateClientService.API.Services.IoT.Commands;
using UpdateClientService.API.Services.IoT.IoTCommand;

namespace UpdateClientService.API.Services.IoT.FileSets
{
    public class KioskFileSetVersionsService : IKioskFileSetVersionsService
    {
        private readonly IIoTCommandClient _iotCommandClient;
        private readonly IStoreService _store;
        private readonly IStateFileService _stateFileService;
        private readonly ILogger<KioskFileSetVersionsService> _logger;

        public KioskFileSetVersionsService(
          IIoTCommandClient iotCommandClient,
          IStoreService store,
          IStateFileService stateFileService,
          ILogger<KioskFileSetVersionsService> logger)
        {
            this._iotCommandClient = iotCommandClient;
            this._store = store;
            this._stateFileService = stateFileService;
            this._logger = logger;
        }

        public async Task<ReportFileSetVersionsResponse> ReportFileSetVersion(
          FileSetPollRequest fileSetPollRequest)
        {
            if (fileSetPollRequest != null)
                return await this.InnerReportFileSetVersions(fileSetPollRequest);
            this._logger.LogErrorWithSource("FileSetPollRequest is null.", nameof(ReportFileSetVersion), "/sln/src/UpdateClientService.API/Services/IoT/FileSets/KioskFileSetVersionsService.cs");
            ReportFileSetVersionsResponse versionsResponse = new ReportFileSetVersionsResponse();
            versionsResponse.StatusCode = HttpStatusCode.InternalServerError;
            return versionsResponse;
        }

        public async Task<ReportFileSetVersionsResponse> ReportFileSetVersions()
        {
            return await this.InnerReportFileSetVersions();
        }

        private async Task<ReportFileSetVersionsResponse> InnerReportFileSetVersions(
          FileSetPollRequest fileSetPollRequest = null)
        {
            ReportFileSetVersionsResponse response = new ReportFileSetVersionsResponse();
            (bool flag, FileSetPollRequestList fileSetPollRequestList) = await this.GetPollRequest();
            if (!flag)
            {
                this._logger.LogErrorWithSource("Couldn't report file set versions", nameof(InnerReportFileSetVersions), "/sln/src/UpdateClientService.API/Services/IoT/FileSets/KioskFileSetVersionsService.cs");
            }
            else
            {
                if (fileSetPollRequest != null)
                {
                    FileSetPollRequest fileSetPollRequest1 = fileSetPollRequestList.FileSetPollRequests.FirstOrDefault<FileSetPollRequest>((Func<FileSetPollRequest, bool>)(x => x.FileSetId == fileSetPollRequest.FileSetId && x.FileSetRevisionId == fileSetPollRequest.FileSetRevisionId));
                    if (fileSetPollRequest1 != null)
                        fileSetPollRequestList.FileSetPollRequests.Remove(fileSetPollRequest1);
                    fileSetPollRequestList.FileSetPollRequests.Add(fileSetPollRequest);
                }
                response = await this.ReportFileSetVersions(fileSetPollRequestList);
            }
            return response;
        }

        private async Task<ReportFileSetVersionsResponse> ReportFileSetVersions(
          FileSetPollRequestList fileSetPollRequestList)
        {
            ReportFileSetVersionsResponse response = new ReportFileSetVersionsResponse();
            if (fileSetPollRequestList == null)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                this._logger.LogErrorWithSource("FileSetPollRequestList is null", nameof(ReportFileSetVersions), "/sln/src/UpdateClientService.API/Services/IoT/FileSets/KioskFileSetVersionsService.cs");
            }
            else
            {
                IoTCommandResponse<ObjectResult> tcommandResponse = await this._iotCommandClient.PerformIoTCommand<ObjectResult>(new IoTCommandModel()
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Version = 3,
                    SourceId = this._store.KioskId.ToString(),
                    Payload = (object)fileSetPollRequestList,
                    Command = CommandEnum.ReportFileSetVersions
                }, new PerformIoTCommandParameters()
                {
                    RequestTimeout = new TimeSpan?(TimeSpan.FromSeconds(15.0)),
                    IoTTopic = "$aws/rules/kioskrestcall",
                    WaitForResponse = true
                });
                if (tcommandResponse != null && tcommandResponse.StatusCode == 200 && tcommandResponse != null)
                {
                    int? statusCode = (int?)tcommandResponse.Payload?.StatusCode;
                    int num = 200;
                    if (statusCode.GetValueOrDefault() == num & statusCode.HasValue)
                    {
                        try
                        {
                            response.ClientFileSetRevisionChangeSets = JsonConvert.DeserializeObject<List<ClientFileSetRevisionChangeSet>>(tcommandResponse.Payload.Value.ToString());
                            goto label_8;
                        }
                        catch (Exception ex)
                        {
                            this._logger.LogErrorWithSource(ex, "Exception deserializing response", nameof(ReportFileSetVersions), "/sln/src/UpdateClientService.API/Services/IoT/FileSets/KioskFileSetVersionsService.cs");
                            goto label_8;
                        }
                    }
                }
                response.StatusCode = HttpStatusCode.InternalServerError;
                this._logger.LogErrorWithSource("Unable to report FileSet revision deployment states.", nameof(ReportFileSetVersions), "/sln/src/UpdateClientService.API/Services/IoT/FileSets/KioskFileSetVersionsService.cs");
            }
        label_8:
            return response;
        }

        private async Task<(bool success, FileSetPollRequestList)> GetPollRequest()
        {
            StateFilesResponse all = await this._stateFileService.GetAll();
            if (all.StatusCode != HttpStatusCode.OK && all.StatusCode != HttpStatusCode.NotFound)
            {
                ILogger<KioskFileSetVersionsService> logger = this._logger;
                if (logger != null)
                    this._logger.LogErrorWithSource("Unable to get StateFiles.", nameof(GetPollRequest), "/sln/src/UpdateClientService.API/Services/IoT/FileSets/KioskFileSetVersionsService.cs");
                return (false, (FileSetPollRequestList)null);
            }
            FileSetPollRequestList setPollRequestList = new FileSetPollRequestList();
            if (all.StateFiles != null)
            {
                foreach (StateFile stateFile in all.StateFiles)
                    setPollRequestList.FileSetPollRequests.Add(new FileSetPollRequest()
                    {
                        FileSetId = stateFile.FileSetId,
                        FileSetRevisionId = stateFile.CurrentRevisionId,
                        FileSetState = stateFile.CurrentFileSetState
                    });
            }
            return (true, setPollRequestList);
        }
    }
}
