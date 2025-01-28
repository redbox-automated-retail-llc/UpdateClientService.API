using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class ExecuteHttpRequestV2 : ICommandIoTController
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<ExecuteHttpRequestV2> _logger;
        private readonly IMqttProxy _mqttProxy;
        private readonly IStoreService _store;

        public ExecuteHttpRequestV2(
          IHttpService httpService,
          ILogger<ExecuteHttpRequestV2> logger,
          IMqttProxy mqttProxy,
          IStoreService store)
        {
            this._httpService = httpService;
            this._logger = logger;
            this._mqttProxy = mqttProxy;
            this._store = store;
        }

        public CommandEnum CommandEnum => CommandEnum.ExecuteHttpRequest;

        public int Version => 2;

        public async Task Execute(IoTCommandModel ioTCommandRequest)
        {
            KioskHttpRequest kioskHttpRequest = JsonConvert.DeserializeObject<KioskHttpRequest>(ioTCommandRequest.Payload.ToJson());
            StringContent stringContent = kioskHttpRequest == null || kioskHttpRequest.JsonBody == null ? (StringContent)null : new StringContent(kioskHttpRequest.JsonBody, Encoding.UTF8, "application/json");
            HttpRequestMessage request1 = this._httpService.GenerateRequest((string)null, kioskHttpRequest.Url, (HttpContent)stringContent, new HttpMethod(kioskHttpRequest.HttpMethod));
            IHttpService httpService = this._httpService;
            HttpRequestMessage request2 = request1;
            int? timeout = new int?();
            IoTCommandModel ioTcommandModel1 = ioTCommandRequest;
            int num1;
            if (ioTcommandModel1 == null)
            {
                num1 = 0;
            }
            else
            {
                bool? logRequest = ioTcommandModel1.LogRequest;
                bool flag = true;
                num1 = logRequest.GetValueOrDefault() == flag & logRequest.HasValue ? 1 : 0;
            }
            IoTCommandModel ioTcommandModel2 = ioTCommandRequest;
            int num2;
            if (ioTcommandModel2 == null)
            {
                num2 = 0;
            }
            else
            {
                bool? logResponse = ioTcommandModel2.LogResponse;
                bool flag = true;
                num2 = logResponse.GetValueOrDefault() == flag & logResponse.HasValue ? 1 : 0;
            }
            APIResponse<object> apiResponse = await httpService.SendRequestAsync<object>(request2, timeout, nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/ExecuteHttpRequestV2.cs", logRequest: num1 != 0, logResponse: num2 != 0);
            MqttResponse<string> mqttResponse1 = new MqttResponse<string>();
            MqttResponse<string> mqttResponse2 = mqttResponse1;
            string str1;
            if (apiResponse == null)
            {
                str1 = (string)null;
            }
            else
            {
                object response = apiResponse.Response;
                str1 = response != null ? response.ToJson() : (string)null;
            }
            mqttResponse2.Data = str1;
            if (apiResponse == null || apiResponse.Response == null)
            {
                mqttResponse1.Data = apiResponse?.ResponseContent;
                if (apiResponse == null || !apiResponse.IsSuccessStatusCode)
                {
                    if (apiResponse?.Errors?.Count > 0)
                        mqttResponse1.Error = "Errors: " + string.Join<string>(",", apiResponse.Errors.Select<Error, string>((Func<Error, string>)(x => x.Message)));
                    IoTCommandModel ioTcommandModel3 = ioTCommandRequest;
                    int num3;
                    if (ioTcommandModel3 == null)
                    {
                        num3 = 0;
                    }
                    else
                    {
                        bool? logResponse = ioTcommandModel3.LogResponse;
                        bool flag = true;
                        num3 = logResponse.GetValueOrDefault() == flag & logResponse.HasValue ? 1 : 0;
                    }
                    string str2;
                    if (num3 == 0)
                    {
                        str2 = string.Empty;
                    }
                    else
                    {
                        string str3;
                        if (apiResponse == null)
                        {
                            str3 = (string)null;
                        }
                        else
                        {
                            HttpResponseMessage httpResponse = apiResponse.HttpResponse;
                            str3 = httpResponse != null ? httpResponse.ToJson() : (string)null;
                        }
                        str2 = "HttpResponse: " + str3;
                    }
                    string str4 = str2;
                    IoTCommandModel ioTcommandModel4 = ioTCommandRequest;
                    int num4;
                    if (ioTcommandModel4 == null)
                    {
                        num4 = 0;
                    }
                    else
                    {
                        bool? logResponse = ioTcommandModel4.LogResponse;
                        bool flag = true;
                        num4 = logResponse.GetValueOrDefault() == flag & logResponse.HasValue ? 1 : 0;
                    }
                    string str5;
                    if (num4 != 0)
                    {
                        IoTCommandModel ioTcommandModel5 = ioTCommandRequest;
                        int num5;
                        if (ioTcommandModel5 == null)
                        {
                            num5 = 0;
                        }
                        else
                        {
                            bool? logRequest = ioTcommandModel5.LogRequest;
                            bool flag = true;
                            num5 = logRequest.GetValueOrDefault() == flag & logRequest.HasValue ? 1 : 0;
                        }
                        if (num5 != 0)
                        {
                            str5 = "Received ioTCommand: " + ioTCommandRequest.ToJson() + ".";
                            goto label_33;
                        }
                    }
                    str5 = string.Empty;
                label_33:
                    string str6 = str5;
                    IoTCommandModel ioTcommandModel6 = ioTCommandRequest;
                    int num6;
                    if (ioTcommandModel6 == null)
                    {
                        num6 = 0;
                    }
                    else
                    {
                        bool? logResponse = ioTcommandModel6.LogResponse;
                        bool flag = true;
                        num6 = logResponse.GetValueOrDefault() == flag & logResponse.HasValue ? 1 : 0;
                    }
                    this._logger.LogErrorWithSource("ExecuteHttpRequestV2 has failed. Result: " + (num6 != 0 ? string.Format("{0}", (object)mqttResponse1) : string.Empty) + "." + str4 + str6, nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/ExecuteHttpRequestV2.cs");
                }
            }
            IoTCommandModel ioTcommandModel7 = ioTCommandRequest;
            int num7;
            if (ioTcommandModel7 == null)
            {
                num7 = 0;
            }
            else
            {
                bool? logResponse = ioTcommandModel7.LogResponse;
                bool flag = true;
                num7 = logResponse.GetValueOrDefault() == flag & logResponse.HasValue ? 1 : 0;
            }
            this._logger.LogInfoWithSource("Executed http v2 request." + (num7 != 0 ? string.Format("Received response: {0}", apiResponse?.Response) : string.Empty), nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/ExecuteHttpRequestV2.cs");
            int num8 = await this._mqttProxy.PublishIoTCommandAsync("redbox/updateservice-instance/" + ioTCommandRequest.SourceId + "/request", new IoTCommandModel()
            {
                RequestId = ioTCommandRequest.RequestId,
                Command = this.CommandEnum,
                MessageType = MessageTypeEnum.Response,
                Version = this.Version,
                SourceId = this._store?.KioskId.ToString(),
                Payload = (object)mqttResponse1.ToJson(),
                LogRequest = new bool?(true)
            }) ? 1 : 0;
        }
    }
}
