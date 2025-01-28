using System;

namespace UpdateClientService.API.Services.IoT
{
    public class UCSRegisterMessage
    {
        public UCSRegisterMessage()
        {
            this.DynamoDbTimeToLive = (int)(DateTime.UtcNow.AddMonths(1) - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public long KioskId { get; set; }

        public string Version { get; set; }

        public DateTime UTCDate { get; set; } = DateTime.UtcNow;

        public int DynamoDbTimeToLive { get; set; }

        public string AssemblyVersion { get; set; }
    }
}
