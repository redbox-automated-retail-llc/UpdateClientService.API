using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Configuration;

namespace UpdateClientService.API.Services.ProxyApi
{
    public class ProxyApi : IProxyApi
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<UpdateClientService.API.Services.ProxyApi.ProxyApi> _logger;
        private readonly IKioskConfiguration _kioskConfiguration;
        private readonly AppSettings _appSettings;
        private readonly List<Header> _headers;
        private const string _proxyAuthHeader = "x-api-key";
        private const string ACTIVITY_ID_HEADER_KEY = "x-redbox-activityid";
        internal const string KIOSK_ID_HEADER_KEY = "x-redbox-kioskid";
        private long _kioskId;

        public ProxyApi(
          IHttpService httpService,
          ILogger<UpdateClientService.API.Services.ProxyApi.ProxyApi> logger,
          IOptionsKioskConfiguration kioskConfiguration,
          IStoreService storeService,
          IOptions<AppSettings> appSettings)
        {
            this._httpService = httpService;
            this._logger = logger;
            this._kioskConfiguration = (IKioskConfiguration)kioskConfiguration;
            this._kioskId = storeService.KioskId;
            this._appSettings = appSettings.Value;
            this._headers = new List<Header>()
      {
        new Header("x-redbox-activityid", Guid.NewGuid().ToString()),
        new Header("x-redbox-kioskid", this._kioskId.ToString()),
        new Header("x-api-key", kioskConfiguration.KioskClientProxyApi.ProxyApiKey)
      };
        }

        public async Task<APIResponse<ApiBaseResponse>> RecordKioskPingFailure(
          KioskPingFailure kioskPingFailure)
        {
            return await this._httpService.SendRequestAsync<ApiBaseResponse>(this.ProxyApiUrl, string.Format("api/kiosk/{0}/pingfailure", (object)this._kioskId), (object)kioskPingFailure, HttpMethod.Post, this._headers, callerMethod: nameof(RecordKioskPingFailure), callerLocation: "/sln/src/UpdateClientService.API/Services/ProxyApi/ProxyApi.cs");
        }

        private string ProxyApiUrl
        {
            get
            {
                return !string.IsNullOrEmpty(this._kioskConfiguration.KioskClientProxyApi.ProxyApiUrl) ? this._kioskConfiguration.KioskClientProxyApi.ProxyApiUrl : this._appSettings.DefaultProxyServiceUrl;
            }
        }
    }
}
