using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Commands.Controller;

namespace UpdateClientService.API.Services.IoT.Commands
{
    public class IoTCommandService : IIoTCommandService
    {
        private readonly List<ICommandIoTController> _commandList;
        private readonly ILogger<IoTCommandService> _logger;
        private readonly IActiveResponseService _activeResponseService;

        public IoTCommandService(
          IActiveResponseService activeResponseService,
          List<ICommandIoTController> commandList,
          ILogger<IoTCommandService> logger)
        {
            this._activeResponseService = activeResponseService;
            this._commandList = commandList;
            this._logger = logger;
        }

        public async Task Execute(byte[] message, string topic)
        {
            await this.IoTCommandCallbackMethodAsync(message, topic);
        }

        private async Task IoTCommandCallbackMethodAsync(byte[] mqttMessage, string mqttTopic)
        {
            try
            {
                string str1 = Encoding.UTF8.GetString(mqttMessage);
                IoTCommandModel iotCommandModel = JsonConvert.DeserializeObject<IoTCommandModel>(str1);
                if (iotCommandModel == null)
                {
                    this._logger.LogErrorWithSource("Error occured in callback from topic " + mqttTopic + ", message failed deserialization: " + str1, nameof(IoTCommandCallbackMethodAsync), "/sln/src/UpdateClientService.API/Services/IoT/Commands/IoTCommandService.cs");
                }
                else
                {
                    string str2;
                    if (!iotCommandModel.LogPayload)
                        str2 = string.Format(", command: {0}, MessageType: {1}, RequestId: {2}, Payload length: {3}", iotCommandModel.Command, iotCommandModel.MessageType, iotCommandModel.RequestId, iotCommandModel.Payload?.ToString()?.Length);
                    else
                        str2 = ", message: " + str1;
                    string str3 = str2;
                    this._logger.LogInfoWithSource("message received topic: " + mqttTopic + str3, nameof(IoTCommandCallbackMethodAsync), "/sln/src/UpdateClientService.API/Services/IoT/Commands/IoTCommandService.cs");
                    if (iotCommandModel.MessageType == MessageTypeEnum.Request)
                    {
                        await this.ExecuteRequest(iotCommandModel);
                    }
                    else
                    {
                        if (iotCommandModel.MessageType != MessageTypeEnum.Response)
                            return;
                        await this.ExecuteResponse(iotCommandModel);
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Error occured in callback from topic " + mqttTopic + ", message was: " + Encoding.UTF8.GetString(mqttMessage), nameof(IoTCommandCallbackMethodAsync), "/sln/src/UpdateClientService.API/Services/IoT/Commands/IoTCommandService.cs");
            }
        }

        private async Task ExecuteRequest(IoTCommandModel iotCommandModel)
        {
            List<ICommandIoTController> commandList1 = this._commandList;
            ICommandIoTController requestedCommand = commandList1 != null ? commandList1.FirstOrDefault((x => x.CommandEnum == iotCommandModel.Command && x.Version == iotCommandModel.Version)) : null;
            if (requestedCommand == null)
            {
                ILogger<IoTCommandService> logger = this._logger;
                CommandEnum command = iotCommandModel.Command;
                int version = iotCommandModel.Version;
                List<ICommandIoTController> commandList2 = this._commandList;
                string json = commandList2 != null ? commandList2.ToJson() : (string)null;
                string str = string.Format("IoT Command: {0}, Version: {1} could not found in registered command list: {2}.", (object)command, (object)version, (object)json);
                this._logger.LogErrorWithSource(str, nameof(ExecuteRequest), "/sln/src/UpdateClientService.API/Services/IoT/Commands/IoTCommandService.cs");
            }
            else
            {
                this._logger.LogInfoWithSource(string.Format("Processing command: {0} for RequestId: {1}", (object)requestedCommand.CommandEnum, (object)iotCommandModel.RequestId), nameof(ExecuteRequest), "/sln/src/UpdateClientService.API/Services/IoT/Commands/IoTCommandService.cs");
                await requestedCommand.Execute(iotCommandModel);
                this._logger.LogInfoWithSource(string.Format("Finished processing command: {0} for RequestId: {1}. Returning. . .", (object)requestedCommand.CommandEnum, (object)iotCommandModel.RequestId), nameof(ExecuteRequest), "/sln/src/UpdateClientService.API/Services/IoT/Commands/IoTCommandService.cs");
            }
        }

        private async Task ExecuteResponse(IoTCommandModel iotCommandModel)
        {
            Action<IoTCommandModel> responseListenerAction = this._activeResponseService.GetResponseListenerAction(iotCommandModel.RequestId);
            if (responseListenerAction == null)
            {
                this._logger.LogInfoWithSource("ActiveResponseListener could not be found for RequestId: " + iotCommandModel.RequestId + ".  Ignoring response.", nameof(ExecuteResponse), "/sln/src/UpdateClientService.API/Services/IoT/Commands/IoTCommandService.cs");
            }
            else
            {
                this._logger.LogInfoWithSource(string.Format("Processing Response Type: {0} for RequestId: {1}", (object)iotCommandModel.Command, (object)iotCommandModel.RequestId), nameof(ExecuteResponse), "/sln/src/UpdateClientService.API/Services/IoT/Commands/IoTCommandService.cs");
                if (responseListenerAction != null)
                    responseListenerAction(iotCommandModel);
                this._activeResponseService.RemoveResponseListener(iotCommandModel.RequestId);
            }
        }
    }
}
