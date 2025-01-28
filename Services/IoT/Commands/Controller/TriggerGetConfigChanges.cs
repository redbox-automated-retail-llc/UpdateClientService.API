using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Configuration;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class TriggerGetConfigChanges : ICommandIoTController
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<TriggerGetConfigChanges> _logger;

        public TriggerGetConfigChanges(
          IConfigurationService configurationService,
          ILogger<TriggerGetConfigChanges> logger)
        {
            this._configurationService = configurationService;
            this._logger = logger;
        }

        public CommandEnum CommandEnum { get; } = CommandEnum.TriggerGetConfigChanges;

        public int Version { get; } = 2;

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            TriggerGetConfigChangesRequest triggerGetConfigChangesRequest = JsonConvert.DeserializeObject<TriggerGetConfigChangesRequest>(ioTCommand.Payload.ToJson());
            try
            {
                ApiBaseResponse configurationSettingChanges = await this._configurationService.TriggerGetKioskConfigurationSettingChanges(triggerGetConfigChangesRequest);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception trying to trigger GetKioskConfigurationSettingChanges", nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/TriggerGetConfigChanges.cs");
            }
        }
    }
}
