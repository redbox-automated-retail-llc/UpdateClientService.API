using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UpdateClientService.API.App;
using UpdateClientService.API.Services.DownloadService;
using UpdateClientService.API.Services.IoT.DownloadFiles;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class GetDownloadFileJobStatusV2 : ICommandIoTController
    {
        private readonly IDownloadFilesService _downloadFilesService;
        private readonly IStoreService _store;
        private readonly ILogger<GetDownloadFileJobStatusV2> _logger;
        private readonly IMqttProxy _mqtt;

        public CommandEnum CommandEnum => CommandEnum.GetDownloadFileJobStatus;

        public int Version => 2;

        public GetDownloadFileJobStatusV2(
          IDownloadFilesService downloadFilesService,
          IStoreService store,
          ILogger<GetDownloadFileJobStatusV2> logger,
          IMqttProxy mqtt)
        {
            this._downloadFilesService = downloadFilesService;
            this._store = store;
            this._logger = logger;
            this._mqtt = mqtt;
        }

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            MqttResponse<List<DownloadData>> result = new MqttResponse<List<DownloadData>>();
            try
            {
                string bitsJobId = ioTCommand.Payload?.ToString();
                MqttResponse<List<DownloadData>> mqttResponse = result;
                mqttResponse.Data = (List<DownloadData>)await this._downloadFilesService.GetDownloadFileJobStatus(bitsJobId);
                mqttResponse = (MqttResponse<List<DownloadData>>)null;
            }
            catch (Exception ex)
            {
                result.Error = ex.GetFullMessage();
                this._logger.LogErrorWithSource(ex, "An unhandled exception occurred while getting download file job status for request " + ioTCommand.RequestId, nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/GetDownloadFileJobStatusV2.cs");
            }
            try
            {
                this._logger.LogInfoWithSource("Publishing response to requestId " + ioTCommand.RequestId + " -> " + result.ToJson(), nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/GetDownloadFileJobStatusV2.cs");
                int num = await this._mqtt.PublishIoTCommandAsync("redbox/updateservice-instance/" + ioTCommand.SourceId + "/request", new IoTCommandModel()
                {
                    RequestId = ioTCommand.RequestId,
                    Command = this.CommandEnum,
                    Version = this.Version,
                    MessageType = MessageTypeEnum.Response,
                    SourceId = this._store.KioskId.ToString(),
                    Payload = (object)result.ToJson()
                }) ? 1 : 0;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "An unhandled exception occurred while publishing the IoT response", nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/GetDownloadFileJobStatusV2.cs");
            }
        }
    }
}
