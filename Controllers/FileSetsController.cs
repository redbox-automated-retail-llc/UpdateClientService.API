using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UpdateClientService.API.Services.FileSets;
using UpdateClientService.API.Services.IoT.FileSets;

namespace UpdateClientService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileSetsController : ControllerBase
    {
        private readonly IFileSetService _fileSetService;
        private readonly IKioskFileSetVersionsService _versionsService;
        private readonly IFileSetCleanup _cleanup;

        public FileSetsController(
          IFileSetService fileSetService,
          IKioskFileSetVersionsService versionsService,
          IFileSetCleanup cleanup)
        {
            this._fileSetService = fileSetService;
            this._versionsService = versionsService;
            this._cleanup = cleanup;
        }

        [HttpPost("invoke")]
        [ProducesResponseType(typeof(OkResult), 200)]
        public async Task<ActionResult> Invoke()
        {
            FileSetsController fileSetsController = this;
            await fileSetsController._fileSetService.ProcessInProgressRevisionChangeSets();
            return (ActionResult)fileSetsController.Ok();
        }

        [HttpPost("versions")]
        [ProducesResponseType(typeof(ReportFileSetVersionsResponse), 200)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ReportVersions([FromQuery] long? executionTimeFrameMs)
        {
            return (ActionResult)(await this._fileSetService.TriggerProcessPendingFileSetVersions(new TriggerReportFileSetVersionsRequest()
            {
                ExecutionTimeFrameMs = executionTimeFrameMs
            })).ToObjectResult();
        }

        [HttpPost("changesets")]
        [ProducesResponseType(typeof(OkResult), 200)]
        [ProducesResponseType(500)]
        [ProducesResponseType(typeof(ProcessChangeSetResponse), 200)]
        [ProducesResponseType(typeof(ProcessChangeSetResponse), 503)]
        [ProducesResponseType(typeof(ProcessChangeSetResponse), 500)]
        public async Task<ActionResult> ProcessChangeSet(ClientFileSetRevisionChangeSet changeSet)
        {
            return (ActionResult)(await this._fileSetService.ProcessChangeSet(changeSet)).ToObjectResult();
        }

        [HttpPost("cleanup")]
        [ProducesResponseType(typeof(OkResult), 200)]
        public async Task<ActionResult> Cleanup()
        {
            FileSetsController fileSetsController = this;
            await fileSetsController._cleanup.Run();
            return (ActionResult)fileSetsController.Ok();
        }
    }
}
