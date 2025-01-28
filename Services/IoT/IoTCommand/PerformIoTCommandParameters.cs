using System;

namespace UpdateClientService.API.Services.IoT.IoTCommand
{
    public class PerformIoTCommandParameters
    {
        public TimeSpan? RequestTimeout { get; set; }

        public string IoTTopic { get; set; }

        public bool WaitForResponse { get; set; } = true;
    }
}
