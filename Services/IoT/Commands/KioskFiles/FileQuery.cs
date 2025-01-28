using Newtonsoft.Json;
using System.IO;

namespace UpdateClientService.API.Services.IoT.Commands.KioskFiles
{
    public class FileQuery
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("searchPattern")]
        public string SearchPattern { get; set; }

        [JsonProperty("searchOptions")]
        public SearchOption SearchOptions { get; set; }
    }
}