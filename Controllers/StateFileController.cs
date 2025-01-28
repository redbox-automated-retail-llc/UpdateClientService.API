using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UpdateClientService.API.Services.FileSets;

namespace UpdateClientService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StateFileController : ControllerBase
    {
        private readonly IStateFileService _stateFileService;

        public StateFileController(IStateFileService stateFileService)
        {
            this._stateFileService = stateFileService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(StateFilesResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GetAll()
        {
            return (ActionResult)(await this._stateFileService.GetAll()).ToObjectResult();
        }

        [HttpGet("{fileSetId}")]
        [ProducesResponseType(typeof(StateFilesResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> Getl([FromRoute] long fileSetId)
        {
            return (ActionResult)(await this._stateFileService.Get(fileSetId)).ToObjectResult();
        }

        [HttpGet("inprogress")]
        [ProducesResponseType(typeof(StateFilesResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GetAllInProgress()
        {
            return (ActionResult)(await this._stateFileService.GetAllInProgress()).ToObjectResult();
        }

        [HttpPost]
        [ProducesResponseType(typeof(StateFileResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> Save([FromBody] StateFile stateFile)
        {
            return (ActionResult)(await this._stateFileService.Save(stateFile)).ToObjectResult();
        }
    }
}
