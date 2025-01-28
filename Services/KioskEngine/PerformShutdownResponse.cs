using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace UpdateClientService.API.Services.KioskEngine
{
    public class PerformShutdownResponse
    {
        [JsonProperty]
        public string Error { get; set; }

        public int? ProcessId { get; set; }

        public ObjectResult ToObjectResult()
        {
            return new ObjectResult((object)this)
            {
                StatusCode = new int?(string.IsNullOrWhiteSpace(this.Error) ? 200 : 500)
            };
        }
    }
}
