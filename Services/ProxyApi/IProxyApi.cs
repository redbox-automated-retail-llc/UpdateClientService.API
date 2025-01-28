using Redbox.NetCore.Middleware.Http;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.ProxyApi
{
    public interface IProxyApi
    {
        Task<APIResponse<ApiBaseResponse>> RecordKioskPingFailure(KioskPingFailure kioskPingFailure);
    }
}
