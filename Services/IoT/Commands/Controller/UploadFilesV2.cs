using Newtonsoft.Json;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Commands.KioskFiles;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class UploadFilesV2 : ICommandIoTController
    {
        private readonly IKioskFilesService _kioskFilesService;
        private readonly IMqttProxy _mqttRepo;
        private readonly IStoreService _store;

        public CommandEnum CommandEnum => CommandEnum.UploadFiles;

        public int Version => 2;

        public UploadFilesV2(
          IKioskFilesService kioskFilesService,
          IMqttProxy mqttRepo,
          IStoreService store)
        {
            this._kioskFilesService = kioskFilesService;
            this._mqttRepo = mqttRepo;
            this._store = store;
        }

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            MqttResponse<string> mqttResponse = await this._kioskFilesService.UploadFilesAsync(JsonConvert.DeserializeObject<KioskUploadFileRequest>(ioTCommand.Payload.ToJson()));
            int num = await this._mqttRepo.PublishIoTCommandAsync("redbox/updateservice-instance/" + ioTCommand.SourceId + "/request", new IoTCommandModel()
            {
                RequestId = ioTCommand.RequestId,
                Command = this.CommandEnum,
                Version = this.Version,
                SourceId = this._store.KioskId.ToString(),
                MessageType = MessageTypeEnum.Response,
                Payload = (object)mqttResponse.ToJson()
            }) ? 1 : 0;
        }
    }
}
