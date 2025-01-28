using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UpdateClientService.API.Services.DataUpdate;

namespace UpdateClientService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataUpdateController : ControllerBase
    {
        private readonly IDataUpdateService _dataUpdateService;

        public DataUpdateController(IDataUpdateService dataUpdateService)
        {
            this._dataUpdateService = dataUpdateService;
        }

        [HttpPost("changes")]
        [ProducesResponseType(typeof(DataUpdateResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetRecordChanges(DataUpdateRequest dataUpdateRequest)
        {
            return (IActionResult)(await this._dataUpdateService.GetRecordChanges(dataUpdateRequest)).ToObjectResult();
        }
    }
}
