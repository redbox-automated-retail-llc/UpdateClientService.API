using System.Threading.Tasks;

namespace UpdateClientService.API.Services.KioskEngine
{
    public interface IKioskEngineService
    {
        Task<PerformShutdownResponse> PerformShutdown(int timeoutMs, int attempts);

        Task<KioskEngineStatus> GetStatus();
    }
}
