using System;
using UpdateClientService.API.Services.IoT.Commands;

namespace UpdateClientService.API.Services
{
    public interface IActiveResponseService
    {
        void AddResponseListener(string requestId, Action<IoTCommandModel> model);

        Action<IoTCommandModel> GetResponseListenerAction(string requestId);

        void RemoveResponseListener(string requestId);
    }
}
