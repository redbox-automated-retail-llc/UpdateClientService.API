using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Commands;

namespace UpdateClientService.API.Services.IoT.IoTCommand
{
    public interface IIoTCommandClient
    {
        Task<IoTCommandResponse<T>> PerformIoTCommand<T>(
          IoTCommandModel request,
          PerformIoTCommandParameters parameters = null)
          where T : ObjectResult;

        Task<string> PerformIoTCommandWithStringResult(
          IoTCommandModel request,
          PerformIoTCommandParameters parameters);
    }
}
