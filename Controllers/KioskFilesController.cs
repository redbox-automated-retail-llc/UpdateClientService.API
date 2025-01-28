using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT;
using UpdateClientService.API.Services.IoT.Commands.KioskFiles;

namespace UpdateClientService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KioskFilesController
    {
        private readonly IKioskFilesService _kioskFilesService;

        public KioskFilesController(IKioskFilesService kioskFilesService)
        {
            this._kioskFilesService = kioskFilesService;
        }

        [HttpGet("peek")]
        [ProducesResponseType(typeof(OkResult), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> PeekKioskFiles([FromQuery] KioskFilePeekRequest request)
        {
            MqttResponse<Dictionary<string, string>> mqttResponse = await this._kioskFilesService.PeekRequestedFilesAsync(request);
            if (!string.IsNullOrWhiteSpace(mqttResponse?.Error))
                return (IActionResult)new ObjectResult((object)mqttResponse.Error)
                {
                    StatusCode = new int?(500)
                };
            return (IActionResult)new ObjectResult((object)mqttResponse.Data)
            {
                StatusCode = new int?(200)
            };
        }

        [HttpGet]
        [ProducesResponseType(typeof(OkResult), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UploadKioskFiles([FromQuery] KioskUploadFileRequest request)
        {
            MqttResponse<string> mqttResponse = await this._kioskFilesService.UploadFilesAsync(request);
            if (!string.IsNullOrWhiteSpace(mqttResponse?.Error))
                return (IActionResult)new ObjectResult((object)mqttResponse.Error)
                {
                    StatusCode = new int?(500)
                };
            return (IActionResult)new ObjectResult((object)mqttResponse.Data)
            {
                StatusCode = new int?(200)
            };
        }
    }
}
