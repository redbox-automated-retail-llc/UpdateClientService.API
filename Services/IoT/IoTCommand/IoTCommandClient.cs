using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Commands;

namespace UpdateClientService.API.Services.IoT.IoTCommand
{
    public class IoTCommandClient : IIoTCommandClient
    {
        private readonly IMqttProxy _mqttRepo;
        private readonly IStoreService _store;
        private readonly AppSettings _appSettings;
        private readonly ILogger<IoTCommandClient> _logger;
        private readonly IActiveResponseService _activeResponseService;
        private readonly TimeSpan _requestTimeoutDefault = TimeSpan.FromSeconds(20.0);

        public IoTCommandClient(
          IMqttProxy mqttRepo,
          IStoreService store,
          IOptions<AppSettings> appSettings,
          ILogger<IoTCommandClient> logger,
          IActiveResponseService activeResponseService)
        {
            this._mqttRepo = mqttRepo;
            this._store = store;
            this._appSettings = appSettings.Value;
            this._logger = logger;
            this._activeResponseService = activeResponseService;
        }

        public async Task<string> PerformIoTCommandWithStringResult(
          IoTCommandModel request,
          PerformIoTCommandParameters parameters)
        {
            return (await this.PerformIoTCommand(request, parameters))?.Payload?.ToString();
        }

        public async Task<IoTCommandResponse<T>> PerformIoTCommand<T>(
          IoTCommandModel request,
          PerformIoTCommandParameters parameters = null)
          where T : ObjectResult
        {
            T resultPayload = default(T);
            if (parameters == null)
                parameters = new PerformIoTCommandParameters();
            IoTCommandModel ioTcommandModel = await this.PerformIoTCommand(request, parameters);
            if (parameters.WaitForResponse)
            {
                try
                {
                    resultPayload = JsonConvert.DeserializeObject<T>(ioTcommandModel?.Payload?.ToString());
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Couldn't deserialize the response from UpdateService for request id " + request?.RequestId, nameof(PerformIoTCommand), "/sln/src/UpdateClientService.API/Services/IoT/IoTCommand/IoTCommandClient.cs");
                }
                return new IoTCommandResponse<T>()
                {
                    StatusCode = (int?)resultPayload?.StatusCode ?? 500,
                    Payload = resultPayload
                };
            }
            bool result;
            bool.TryParse(ioTcommandModel?.Payload?.ToString(), out result);
            int num = result ? 200 : 500;
            return new IoTCommandResponse<T>()
            {
                StatusCode = num
            };
        }

        private async Task<IoTCommandModel> PerformIoTCommand(
          IoTCommandModel request,
          PerformIoTCommandParameters parameters)
        {
            if (string.IsNullOrWhiteSpace(request.SourceId))
                request.SourceId = this._store.KioskId.ToString();
            this.AdjustIoTTopicIfNeeded(request, parameters);
            PerformIoTCommandParameters tcommandParameters = parameters;
            return (tcommandParameters != null ? (tcommandParameters.WaitForResponse ? 1 : 0) : 0) != 0 ? await this.PublishIoTCommandAndWaitForResponse(request, parameters) : await this.PublishIoTCommandAndReturn(request, parameters);
        }

        private async Task<IoTCommandModel> PublishIoTCommandAndReturn(
          IoTCommandModel request,
          PerformIoTCommandParameters parameters)
        {
            bool flag = await this.PublishIoTCommandAsync(request, parameters);
            return new IoTCommandModel()
            {
                RequestId = request.RequestId,
                MessageType = MessageTypeEnum.Response,
                Command = request.Command,
                Version = request.Version,
                Payload = (object)flag.ToString(),
                SourceId = this._store.KioskId.ToString()
            };
        }

        private async Task<IoTCommandModel> PublishIoTCommandAndWaitForResponse(
          IoTCommandModel request,
          PerformIoTCommandParameters parameters)
        {
            IoTCommandModel response = (IoTCommandModel)null;
            AsyncManualResetEvent mre = new AsyncManualResetEvent(false);
            try
            {
                this._activeResponseService.AddResponseListener(request.RequestId, (Action<IoTCommandModel>)(responseIotCommandModel =>
                {
                    response = responseIotCommandModel;
                    mre.Set();
                    this._logger.LogInfoWithSource(string.Format("Received response from update service for request id {0}, IotCommand: {1},  IoT Topic: {2}.", (object)request?.RequestId, (object)request?.Command, (object)parameters?.IoTTopic), nameof(PublishIoTCommandAndWaitForResponse), "/sln/src/UpdateClientService.API/Services/IoT/IoTCommand/IoTCommandClient.cs");
                }));
                TimeSpan iotCommandTimeout = parameters.RequestTimeout ?? this._requestTimeoutDefault;
                if (await this.PublishIoTCommandAsync(request, parameters))
                {
                    int num = await mre.WaitAsyncAndDisposeOnFinish(iotCommandTimeout, request) ? 1 : 0;
                }
                if (response == null)
                    response = new IoTCommandModel()
                    {
                        RequestId = request.RequestId,
                        MessageType = MessageTypeEnum.Response,
                        Command = request.Command,
                        Version = request.Version,
                        Payload = (object)new StatusCodeResult(504).ToJson(),
                        SourceId = this._store.KioskId.ToString()
                    };
                iotCommandTimeout = new TimeSpan();
            }
            finally
            {
                this._activeResponseService.RemoveResponseListener(request.RequestId);
            }
            return response;
        }

        private async Task<bool> PublishIoTCommandAsync(
          IoTCommandModel request,
          PerformIoTCommandParameters parameters)
        {
            return await this._mqttRepo.PublishIoTCommandAsync(parameters?.IoTTopic, request);
        }

        private void AdjustIoTTopicIfNeeded(
          IoTCommandModel request,
          PerformIoTCommandParameters parameters)
        {
            if (!string.IsNullOrEmpty(parameters?.IoTTopic))
                return;
            string instanceString = this.GetInstanceString(request?.ReturnServerId);
            parameters.IoTTopic = "redbox/updateservice-instance/" + instanceString + "/request";
            this._logger.LogInfoWithSource("Using IoT Topic: " + parameters.IoTTopic + " for request id: " + request.RequestId, nameof(AdjustIoTTopicIfNeeded), "/sln/src/UpdateClientService.API/Services/IoT/IoTCommand/IoTCommandClient.cs");
        }

        private string GetInstanceString(string stringInstanceNumberOverride = null)
        {
            return stringInstanceNumberOverride;
        }
    }
}
