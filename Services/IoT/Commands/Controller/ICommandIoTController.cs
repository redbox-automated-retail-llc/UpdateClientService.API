using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public interface ICommandIoTController
    {
        CommandEnum CommandEnum { get; }

        int Version { get; }

        Task Execute(IoTCommandModel ioTCommand);
    }
}
