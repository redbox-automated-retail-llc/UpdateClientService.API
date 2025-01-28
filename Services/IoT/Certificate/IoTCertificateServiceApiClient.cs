using Microsoft.Extensions.Options;
using Redbox.NetCore.Middleware.Http;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Certificate
{
    public class IoTCertificateServiceApiClient : IIoTCertificateServiceApiClient
    {
        private readonly IHttpService _httpService;
        private readonly IStoreService _store;
        private readonly AppSettings _appSettings;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public IoTCertificateServiceApiClient(
          IHttpService httpService,
          IStoreService store,
          IOptions<AppSettings> appSettings)
        {
            this._httpService = httpService;
            this._store = store;
            this._appSettings = appSettings.Value;
            this._httpService.Timeout = 90000;
        }

        public async Task<IsCertificateValidResponse> IsCertificateValid(
          string kioskId,
          string thingType,
          string password,
          string certificateId)
        {
            IsCertificateValidResponse response = new IsCertificateValidResponse()
            {
                IsValid = new bool?()
            };
            var data = new
            {
                type = thingType,
                name = kioskId,
                thingGroupName = this._store.Market,
                certificateId = certificateId
            };
            IHttpService httpService = this._httpService;
            string tcertificateServiceUrl = this._appSettings.IoTCertificateServiceUrl;
            var requestObject = data;
            HttpMethod post = HttpMethod.Post;
            List<Header> headers = new List<Header>();
            headers.Add(new Header("x-api-key", this._appSettings.IoTCertificateServiceApiKey));
            headers.Add(new Header(nameof(password), password));
            int? timeout = new int?();
            APIResponse<bool> apiResponse = await httpService.SendRequestAsync<bool>(tcertificateServiceUrl, "api/thing-types/certificate-valid", (object)requestObject, post, headers, timeout: timeout, callerMethod: nameof(IsCertificateValid), callerLocation: "/sln/src/UpdateClientService.API/Services/IoT/Certificate/IoTCertificateServiceApiClient.cs");
            response.IsValid = !apiResponse.IsSuccessStatusCode ? new bool?() : new bool?(apiResponse.Response);
            return response;
        }

        public async Task<IoTCertificateServiceResponse> GetCertificates(
          string kioskId,
          string thingType,
          string password)
        {
            await this._lock.WaitAsync();
            var data = new
            {
                type = thingType,
                name = kioskId,
                thingGroupName = this._store.Market
            };
            IHttpService httpService = this._httpService;
            string tcertificateServiceUrl = this._appSettings.IoTCertificateServiceUrl;
            var requestObject = data;
            HttpMethod post = HttpMethod.Post;
            List<Header> headers = new List<Header>();
            headers.Add(new Header("x-api-key", this._appSettings.IoTCertificateServiceApiKey));
            headers.Add(new Header(nameof(password), password));
            int? timeout = new int?();
            APIResponse<IoTCertificateServiceResponse> apiResponse = await httpService.SendRequestAsync<IoTCertificateServiceResponse>(tcertificateServiceUrl, "api/thing-types/get-certificates", (object)requestObject, post, headers, timeout: timeout, callerMethod: nameof(GetCertificates), callerLocation: "/sln/src/UpdateClientService.API/Services/IoT/Certificate/IoTCertificateServiceApiClient.cs", logResponse: false);
            this._lock.Release();
            return apiResponse?.Response;
        }

        public async Task<IoTCertificateServiceResponse> GenerateNewCertificates(
          string kioskId,
          string thingType,
          string password)
        {
            APIResponse<IoTCertificateServiceResponse> apiResponse;
            try
            {
                await this._lock.WaitAsync();
                var data = new
                {
                    type = thingType,
                    name = kioskId,
                    thingGroupName = this._store.Market
                };
                IHttpService httpService = this._httpService;
                string tcertificateServiceUrl = this._appSettings.IoTCertificateServiceUrl;
                var requestObject = data;
                HttpMethod post = HttpMethod.Post;
                List<Header> headers = new List<Header>();
                headers.Add(new Header("x-api-key", this._appSettings.IoTCertificateServiceApiKey));
                headers.Add(new Header(nameof(password), password));
                int? timeout = new int?();
                apiResponse = await httpService.SendRequestAsync<IoTCertificateServiceResponse>(tcertificateServiceUrl, "api/thing-types/certificates", (object)requestObject, post, headers, timeout: timeout, callerMethod: nameof(GenerateNewCertificates), callerLocation: "/sln/src/UpdateClientService.API/Services/IoT/Certificate/IoTCertificateServiceApiClient.cs", logResponse: false);
            }
            finally
            {
                this._lock.Release();
            }
            return apiResponse?.Response;
        }
    }
}
