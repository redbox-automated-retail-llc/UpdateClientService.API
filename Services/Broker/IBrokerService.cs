using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UpdateClientService.API.Controllers.Models;

namespace UpdateClientService.API.Services.Broker
{
    public interface IBrokerService
    {
        Task<IActionResult> Ping(string appName, string messageId, PingRequest pingRequest);

        Task<IActionResult> Register(string appName, string messageId, RegisterRequest request);

        Task<IActionResult> Unregister(
          string appName,
          string messageId,
          UnRegisterRequest unregisterRequest);
    }
}
