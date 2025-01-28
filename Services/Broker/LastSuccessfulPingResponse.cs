using Redbox.NetCore.Middleware.Http;
using System;

namespace UpdateClientService.API.Services.Broker
{
    public class LastSuccessfulPingResponse : ApiBaseResponse
    {
        public DateTime? LastSuccessfulPingUTC { get; set; }
    }
}
