using Newtonsoft.Json;
using System;

namespace UpdateClientService.API.Services.IoT.Commands.KioskFiles
{
    public class KioskFilePeekRequest
    {
        [JsonProperty("date")]
        public DateTime? Date { get; set; }

        [JsonProperty("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonProperty("fileType")]
        public string FileType { get; set; }

        [JsonProperty("fileQuery")]
        public FileQuery FileQuery { get; set; }
    }
}
