using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Commands
{
    public class IotCommandDispatch : IIotCommandDispatch
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<IoTCommandService> _logger;

        public IotCommandDispatch(
          IServiceScopeFactory serviceScopeFactory,
          ILogger<IoTCommandService> logger)
        {
            this._serviceScopeFactory = serviceScopeFactory;
            this._logger = logger;
        }

        public async Task Execute(byte[] message, string topic)
        {
            this._logger.LogInfoWithSource("Execute(" + topic, nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/IotCommandDispatch.cs");
            using (IServiceScope scope = this._serviceScopeFactory.CreateScope())
                await ServiceProviderServiceExtensions.GetService<IIoTCommandService>(scope.ServiceProvider).Execute(message, topic);
        }
    }
}
