using Newtonsoft.Json;
using System;

namespace UpdateClientService.API.Controllers.Models
{
    public class PingRequest
    {
        [JsonIgnore]
        public Guid MessageId { get; set; }

        public long KioskId { get; set; }

        public string AppName { get; set; }

        public string UTCDate { get; set; }

        public string LocalDate { get; set; }

        public long TTL
        {
            get
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
                dateTimeOffset = dateTimeOffset.AddDays(30.0);
                return dateTimeOffset.ToUnixTimeSeconds();
            }
        }

        public bool? SendToTable { get; set; }
    }
}
