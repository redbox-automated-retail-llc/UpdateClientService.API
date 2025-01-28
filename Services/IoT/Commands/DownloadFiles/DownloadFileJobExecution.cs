using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace UpdateClientService.API.Services.IoT.Commands.DownloadFiles
{
    public class DownloadFileJobExecution
    {
        public string DownloadFileJobExecutionId { get; set; }

        public string DownloadFileJobId { get; set; }

        public long KioskId { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public JobStatus Status { get; set; }

        public DateTime CompletedOn { get; set; }

        public DateTime CompletedOnUtc => this.CompletedOn.ToUniversalTime();

        public DateTime EventTime { get; set; }

        public DateTime EventTimeUtc => this.EventTime.ToUniversalTime();
    }
}
