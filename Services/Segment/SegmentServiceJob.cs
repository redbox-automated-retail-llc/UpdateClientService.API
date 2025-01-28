using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.Segment
{
    public class SegmentServiceJob : ISegmentServiceJob, IInvocable
    {
        private readonly ILogger<SegmentServiceJob> _logger;
        private readonly ISegmentService _segmentService;

        public SegmentServiceJob(ILogger<SegmentServiceJob> logger, ISegmentService segmentService)
        {
            this._logger = logger;
            this._segmentService = segmentService;
        }

        public async Task Invoke()
        {
            this._logger.LogInfoWithSource("Invoking SegmentService.UpdateKioskSegmentsIfNeeded", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/Segment/SegmentServiceJob.cs");
            int num = await this._segmentService.UpdateKioskSegmentsIfNeeded() ? 1 : 0;
        }
    }
}
