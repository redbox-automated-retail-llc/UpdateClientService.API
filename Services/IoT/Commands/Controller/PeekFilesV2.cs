using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Commands.KioskFiles;
using UpdateClientService.API.Services.IoT.IoTCommand;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class PeekFilesV2 : ICommandIoTController
    {
        private readonly IKioskFilesService _kioskFilesService;
        private readonly IMqttProxy _mqttRepo;
        private readonly IIoTCommandClient _iotCommandClient;
        private readonly IStoreService _store;

        public CommandEnum CommandEnum => CommandEnum.PeekFiles;

        public int Version => 2;

        public PeekFilesV2(
          IKioskFilesService kioskFilesService,
          IMqttProxy mqttRepo,
          IIoTCommandClient iotCommandClient,
          IStoreService store)
        {
            this._kioskFilesService = kioskFilesService;
            this._mqttRepo = mqttRepo;
            this._iotCommandClient = iotCommandClient;
            this._store = store;
        }

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            MqttResponse<Dictionary<string, string>> mqttResponse = await this._kioskFilesService.PeekRequestedFilesAsync(JsonConvert.DeserializeObject<KioskFilePeekRequest>(ioTCommand.Payload.ToJson()));
            int num = await this._mqttRepo.PublishIoTCommandAsync("redbox/updateservice-instance/" + ioTCommand.SourceId + "/request", new IoTCommandModel()
            {
                RequestId = ioTCommand.RequestId,
                Command = this.CommandEnum,
                Version = this.Version,
                MessageType = MessageTypeEnum.Response,
                SourceId = this._store.KioskId.ToString(),
                Payload = (object)mqttResponse.ToJson(),
                LogRequest = new bool?(false)
            }) ? 1 : 0;
        }
    }
}
