using System;

namespace UpdateClientService.API.Services.ProxyApi
{
    public class KioskPingFailure
    {
        public long KioskId { get; set; }

        public DateTime LastSuccessfulPingUTC { get; set; }

        public DateTime LastUpdateUTC { get; set; }
    }
}
