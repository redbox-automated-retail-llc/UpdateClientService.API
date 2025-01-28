using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT
{
    public class MqttProxyJob : IInvocable
    {
        private ILogger<MqttProxyJob> _logger;
        private IMqttProxy _mqttProxy;

        public MqttProxyJob(ILogger<MqttProxyJob> logger, IMqttProxy mqttProxy)
        {
            this._logger = logger;
            this._mqttProxy = mqttProxy;
        }

        public async Task Invoke()
        {
            int num = await this._mqttProxy.CheckConnectionAsync() ? 1 : 0;
        }
    }
}
