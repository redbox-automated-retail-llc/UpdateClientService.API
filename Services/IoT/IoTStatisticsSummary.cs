using System;
using System.Collections.Generic;

namespace UpdateClientService.API.Services.IoT
{
    public class IoTStatisticsSummary
    {
        public string Date { get; set; }

        public int ConnectionCount { get; set; }

        public int ConnectionAttemptsCount { get; set; }

        public TimeSpan TotalConnectionAttemptsDuration { get; set; }

        public List<ConnectionException> ConnectionExceptions { get; set; }
    }
}
