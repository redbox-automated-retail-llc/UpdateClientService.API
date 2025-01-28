using System;
using System.Collections.Generic;

namespace UpdateClientService.API.Services.IoT
{
    public class IoTConnectionAttempt
    {
        public DateTime? StartConnectionAttempt { get; set; }

        public DateTime? LatestConnectionAttempt { get; set; }

        public DateTime? Connected { get; set; }

        public DateTime? Disconnected { get; set; }

        public TimeSpan? ConnectionAttemptsDuration
        {
            get
            {
                DateTime? nullable = this.Connected.HasValue ? this.Connected : this.LatestConnectionAttempt;
                return !this.StartConnectionAttempt.HasValue || !nullable.HasValue ? new TimeSpan?() : new TimeSpan?(nullable.Value - this.StartConnectionAttempt.Value);
            }
        }

        public int ConnectionAttempts { get; set; }

        public Dictionary<string, int> ConnectionExceptions { get; set; }
    }
}
