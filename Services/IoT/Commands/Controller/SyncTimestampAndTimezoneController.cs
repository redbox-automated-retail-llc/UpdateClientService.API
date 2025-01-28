using System.Threading.Tasks;
using UpdateClientService.API.Services.Kernel;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class SyncTimestampAndTimezoneController : ICommandIoTController
    {
        private readonly IKernelService _kernelService;

        public SyncTimestampAndTimezoneController(IKernelService kernelService)
        {
            this._kernelService = kernelService;
        }

        public CommandEnum CommandEnum => CommandEnum.SyncTimestampAndTimezone;

        public int Version => 1;

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            this._kernelService.SyncTimeAndTimezone(ioTCommand.Payload);
        }
    }
}
