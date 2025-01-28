using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Segment;

namespace UpdateClientService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SegmentsController : ControllerBase
    {
        private readonly ISegmentService _segmentService;

        public SegmentsController(ISegmentService segmentService)
        {
            this._segmentService = segmentService;
        }

        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(503)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateKioskSegmentsFromUpdateService()
        {
            return (IActionResult)new StatusCodeResult((int)(await this._segmentService.UpdateKioskSegmentsFromUpdateService()).StatusCode);
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetKioskSegments()
        {
            return (IActionResult)(await this._segmentService.GetKioskSegments()).ToObjectResult();
        }
    }
}
