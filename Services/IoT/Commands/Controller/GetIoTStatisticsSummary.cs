using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class GetIoTStatisticsSummary : ICommandIoTController
    {
        private readonly ILogger<GetIoTStatisticsSummary> _logger;
        private readonly IStoreService _store;
        private readonly IMqttProxy _mqttProxy;
        private readonly IIoTStatisticsService _ioTStatisticsService;

        public GetIoTStatisticsSummary(
          ILogger<GetIoTStatisticsSummary> logger,
          IIoTStatisticsService ioTStatisticsService,
          IStoreService store,
          IMqttProxy mqttProxy)
        {
            this._logger = logger;
            this._ioTStatisticsService = ioTStatisticsService;
            this._store = store;
            this._mqttProxy = mqttProxy;
        }

        public CommandEnum CommandEnum => CommandEnum.GetIotStatisticsSummary;

        public int Version => 2;

        public async Task Execute(IoTCommandModel ioTCommandRequest)
        {
            try
            {
                this._logger.LogInfoWithSource("Getting IoTStatisticsSummary", nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/GetIoTStatisticsSummary.cs");
                IoTStatisticsSummaryResponse tstatisticsSummaryResponse = await this._ioTStatisticsService.GetIoTStatisticsSummaryResponse();
                MqttResponse<List<IoTStatisticsSummary>> mqttResponse = new MqttResponse<List<IoTStatisticsSummary>>();
                if (tstatisticsSummaryResponse != null && tstatisticsSummaryResponse.StatusCode == HttpStatusCode.OK && tstatisticsSummaryResponse.ioTStatisticsSummaries != null)
                    mqttResponse.Data = tstatisticsSummaryResponse.ioTStatisticsSummaries;
                else
                    mqttResponse.Error = "Error getting IoTStatisticsSummary";
                string json = mqttResponse.ToJson();
                int num = await this._mqttProxy.PublishIoTCommandAsync("redbox/updateservice-instance/" + ioTCommandRequest.SourceId + "/request", new IoTCommandModel()
                {
                    RequestId = ioTCommandRequest.RequestId,
                    Command = this.CommandEnum,
                    MessageType = MessageTypeEnum.Response,
                    Version = this.Version,
                    SourceId = this._store?.KioskId.ToString(),
                    Payload = (object)json,
                    LogRequest = new bool?(true)
                }) ? 1 : 0;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception getting IoT statistics summary", nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/GetIoTStatisticsSummary.cs");
            }
        }
    }
}
