using Redbox.NetCore.Middleware.Http;
using System.Collections.Generic;

namespace UpdateClientService.API.Services.Segment
{
    public class KioskSegmentsResponse : ApiBaseResponse
    {
        public List<KioskSegmentModel> Segments { get; set; } = new List<KioskSegmentModel>();
    }
}
