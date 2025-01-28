using System;

namespace UpdateClientService.API.Services.Broker
{
    public class PingStatistic
    {
        public DateTime StartInterval { get; set; }

        public DateTime StartIntervalUTC { get; set; }

        public DateTime EndInterval { get; set; }

        public DateTime EndIntervalUTC { get; set; }

        public bool PingSuccess { get; set; }

        public int PingCount { get; set; }
    }
}
