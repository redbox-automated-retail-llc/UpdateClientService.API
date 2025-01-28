using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Configuration;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class TriggerUpdateConfigStatus : ICommandIoTController
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<TriggerUpdateConfigStatus> _logger;

        public TriggerUpdateConfigStatus(
          IConfigurationService configurationService,
          ILogger<TriggerUpdateConfigStatus> logger)
        {
            this._configurationService = configurationService;
            this._logger = logger;
        }

        public CommandEnum CommandEnum { get; } = CommandEnum.TriggerUpdateConfigStatus;

        public int Version { get; } = 2;

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            TriggerUpdateConfigStatusRequest triggerUpdateConfigStatusRequest = JsonConvert.DeserializeObject<TriggerUpdateConfigStatusRequest>(ioTCommand.Payload.ToJson());
            try
            {
                ApiBaseResponse apiBaseResponse = await this._configurationService.TriggerUpdateConfigurationStatus(triggerUpdateConfigStatusRequest);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception trying to trigger UpdateConfigurationStatus", nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/TriggerUpdateConfigStatus.cs");
            }
        }
    }
}
