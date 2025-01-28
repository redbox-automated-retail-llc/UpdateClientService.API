using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Threading.Tasks;
using UpdateClientService.API.Controllers.Models;
using UpdateClientService.API.Services.IoT;
using UpdateClientService.API.Services.IoT.Commands;
using UpdateClientService.API.Services.IoT.IoTCommand;

namespace UpdateClientService.API.Services.Broker
{
    public class BrokerService : IBrokerService
    {
        private readonly IIoTCommandClient _ioTCommandClient;
        private readonly ILogger<BrokerService> _logger;
        private readonly IPingStatisticsService _pingStatisticsService;

        public BrokerService(
          ILogger<BrokerService> logger,
          IIoTCommandClient ioTCommandClient,
          IPingStatisticsService pingStatisticsService)
        {
            this._logger = logger;
            this._ioTCommandClient = ioTCommandClient;
            this._pingStatisticsService = pingStatisticsService;
        }

        public async Task<IActionResult> Ping(
          string appName,
          string messageId,
          PingRequest pingRequest)
        {
            try
            {
                PerformIoTCommandParameters parameters = new PerformIoTCommandParameters()
                {
                    IoTTopic = "$aws/rules/kioskping",
                    WaitForResponse = false
                };
                IIoTCommandClient ioTcommandClient = this._ioTCommandClient;
                IoTCommandModel request = new IoTCommandModel();
                request.Version = 1;
                request.RequestId = messageId;
                request.AppName = appName;
                request.Command = CommandEnum.KioskPing;
                request.Payload = (object)pingRequest;
                request.QualityOfServiceLevel = QualityOfServiceLevel.AtMostOnce;
                PerformIoTCommandParameters parameters1 = parameters;
                IoTCommandResponse<ObjectResult> ioTCommandResponse = await ioTcommandClient.PerformIoTCommand<ObjectResult>(request, parameters1);
                IPingStatisticsService statisticsService = this._pingStatisticsService;
                IoTCommandResponse<ObjectResult> tcommandResponse = ioTCommandResponse;
                int num1 = tcommandResponse != null ? (tcommandResponse.StatusCode == 200 ? 1 : 0) : 0;
                int num2 = await statisticsService.RecordPingStatistic(num1 != 0) ? 1 : 0;
                return (IActionResult)this.ProcessIoTCommandResponse<ObjectResult>(ioTCommandResponse, parameters);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception executing Ping", nameof(Ping), "/sln/src/UpdateClientService.API/Services/Broker/BrokerService.cs");
                return (IActionResult)new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> Register(
          string appName,
          string messageId,
          RegisterRequest request)
        {
            try
            {
                PerformIoTCommandParameters parameters = new PerformIoTCommandParameters()
                {
                    IoTTopic = "$aws/rules/kioskrestcall",
                    WaitForResponse = true
                };
                IIoTCommandClient ioTcommandClient = this._ioTCommandClient;
                IoTCommandModel request1 = new IoTCommandModel();
                request1.Version = 1;
                request1.RequestId = messageId;
                request1.AppName = appName;
                request1.Command = CommandEnum.BrokerRegister;
                request1.Payload = (object)request;
                request1.QualityOfServiceLevel = QualityOfServiceLevel.AtMostOnce;
                PerformIoTCommandParameters parameters1 = parameters;
                return (IActionResult)this.ProcessIoTCommandResponse<ObjectResult>(await ioTcommandClient.PerformIoTCommand<ObjectResult>(request1, parameters1), parameters);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception executing Register", nameof(Register), "/sln/src/UpdateClientService.API/Services/Broker/BrokerService.cs");
                return (IActionResult)new StatusCodeResult(500);
            }
        }

        public async Task<IActionResult> Unregister(
          string appName,
          string messageId,
          UnRegisterRequest unregisterRequest)
        {
            try
            {
                PerformIoTCommandParameters parameters = new PerformIoTCommandParameters()
                {
                    IoTTopic = "$aws/rules/kioskrestcall",
                    WaitForResponse = true
                };
                IIoTCommandClient ioTcommandClient = this._ioTCommandClient;
                IoTCommandModel request = new IoTCommandModel();
                request.Version = 1;
                request.RequestId = messageId;
                request.AppName = appName;
                request.Command = CommandEnum.BrokerUnRegister;
                request.Payload = (object)unregisterRequest;
                request.QualityOfServiceLevel = QualityOfServiceLevel.AtMostOnce;
                PerformIoTCommandParameters parameters1 = parameters;
                return (IActionResult)this.ProcessIoTCommandResponse<ObjectResult>(await ioTcommandClient.PerformIoTCommand<ObjectResult>(request, parameters1), parameters);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception executing Unregister", nameof(Unregister), "/sln/src/UpdateClientService.API/Services/Broker/BrokerService.cs");
                return (IActionResult)new StatusCodeResult(500);
            }
        }

        private StatusCodeResult ProcessIoTCommandResponse<T>(
          IoTCommandResponse<T> response,
          PerformIoTCommandParameters parameters = null)
          where T : ObjectResult
        {
            if (parameters == null || parameters.WaitForResponse)
                return this.ToStatusCodeResult((object)response.Payload);
            return this.ToStatusCodeResult((object)new ObjectResult((object)null)
            {
                StatusCode = new int?(response != null ? response.StatusCode : 500)
            });
        }

        private StatusCodeResult ToStatusCodeResult(object objectResult)
        {
            return new StatusCodeResult((objectResult is ObjectResult objectResult1 ? (objectResult1.StatusCode) : new int?()) ?? 500);
        }
    }
}
