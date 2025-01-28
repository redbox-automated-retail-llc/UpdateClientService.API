using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using UpdateClientService.API.Services;

namespace UpdateClientService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private ILogger<StatusController> _logger;
        private IStatusService _status;

        public StatusController(ILogger<StatusController> logger, IStatusService status)
        {
            this._logger = logger;
            this._status = status;
        }

        [HttpGet("versions")]
        [ProducesResponseType(200, Type = typeof(FileVersionDataResponse))]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public IActionResult FileVersions()
        {
            return (IActionResult)this._status.GetFileVersions().ToObjectResult();
        }

        [HttpPost("versions")]
        [ProducesResponseType(200, Type = typeof(FileVersionDataResponse))]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public IActionResult FileVersions(IEnumerable<string> filePaths)
        {
            return (IActionResult)this._status.GetFileVersions(filePaths).ToObjectResult();
        }
    }
}
