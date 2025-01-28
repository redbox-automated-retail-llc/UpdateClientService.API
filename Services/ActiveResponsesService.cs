using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Collections.Concurrent;
using UpdateClientService.API.Services.IoT.Commands;

namespace UpdateClientService.API.Services
{
    public class ActiveResponsesService : IActiveResponseService
    {
        private ConcurrentDictionary<string, Action<IoTCommandModel>> _activeResponses = new ConcurrentDictionary<string, Action<IoTCommandModel>>();
        private ILogger<ActiveResponsesService> _logger;

        public ActiveResponsesService(ILogger<ActiveResponsesService> logger) => this._logger = logger;

        public void AddResponseListener(string requestId, Action<IoTCommandModel> model)
        {
            bool flag = this._activeResponses.TryAdd(requestId, model);
            this._logger.LogInfoWithSource(string.Format("Adding {0} to Active Listeners - success = {1}", (object)requestId, (object)flag), nameof(AddResponseListener), "/sln/src/UpdateClientService.API/Services/ActiveResponsesService.cs");
        }

        public Action<IoTCommandModel> GetResponseListenerAction(string requestId)
        {
            Action<IoTCommandModel> responseListenerAction;
            if (this._activeResponses.TryGetValue(requestId, out responseListenerAction))
                return responseListenerAction;
            this._logger.LogWarningWithSource("Failed To Find RequestId: " + requestId + " in Active Responses.", nameof(GetResponseListenerAction), "/sln/src/UpdateClientService.API/Services/ActiveResponsesService.cs");
            return (Action<IoTCommandModel>)null;
        }

        public void RemoveResponseListener(string requestId)
        {
            bool flag = this._activeResponses.TryRemove(requestId, out Action<IoTCommandModel> _);
            this._logger.LogInfoWithSource(string.Format("Removing {0} from Active Listeners - success = {1}", (object)requestId, (object)flag), nameof(RemoveResponseListener), "/sln/src/UpdateClientService.API/Services/ActiveResponsesService.cs");
        }
    }
}
