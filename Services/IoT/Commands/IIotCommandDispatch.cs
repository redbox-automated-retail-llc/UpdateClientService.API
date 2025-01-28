using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Commands
{
    public interface IIotCommandDispatch
    {
        Task Execute(byte[] message, string topic);
    }
}
