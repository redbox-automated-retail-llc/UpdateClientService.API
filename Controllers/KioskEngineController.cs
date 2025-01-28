using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UpdateClientService.API.Services.KioskEngine;

namespace UpdateClientService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KioskEngineController : ControllerBase
    {
        private readonly IKioskEngineService _kioskEngineService;

        public KioskEngineController(IKioskEngineService kioskEngineService)
        {
            this._kioskEngineService = kioskEngineService;
        }

        [HttpPost("shutdown")]
        [ProducesResponseType(typeof(PerformShutdownResponse), 200)]
        [ProducesResponseType(typeof(PerformShutdownResponse), 500)]
        public async Task<ActionResult<PerformShutdownResponse>> PerformShutdown(
          int timeoutMs,
          int attempts)
        {
            return (ActionResult<PerformShutdownResponse>)(ActionResult)(await this._kioskEngineService.PerformShutdown(timeoutMs, attempts)).ToObjectResult();
        }

        [HttpGet("isrunning")]
        [ProducesResponseType(typeof(KioskEngineStatus), 200)]
        [ProducesResponseType(typeof(KioskEngineStatus), 500)]
        [ProducesResponseType(typeof(KioskEngineStatus), 503)]
        public async Task<ActionResult<KioskEngineStatus>> IsKioskEngineRunning()
        {
            return (ActionResult<KioskEngineStatus>)(ActionResult)(await this._kioskEngineService.GetStatus()).ToObjectResult();
        }
    }
}
