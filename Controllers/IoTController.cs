using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT;

namespace UpdateClientService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IoTController : ControllerBase
    {
        private readonly IIoTStatisticsService _ioTStatisticsService;

        public IoTController(IIoTStatisticsService ioTStatisticsService)
        {
            this._ioTStatisticsService = ioTStatisticsService;
        }

        [HttpGet("statisticssummary")]
        [ProducesResponseType(typeof(IoTStatisticsSummaryResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetIoTStatisticsSummaryResponse()
        {
            return (IActionResult)(await this._ioTStatisticsService.GetIoTStatisticsSummaryResponse()).ToObjectResult();
        }
    }
}
