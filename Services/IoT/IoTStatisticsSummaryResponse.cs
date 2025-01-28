using Redbox.NetCore.Middleware.Http;
using System.Collections.Generic;

namespace UpdateClientService.API.Services.IoT
{
    public class IoTStatisticsSummaryResponse : ApiBaseResponse
    {
        public List<IoTStatisticsSummary> ioTStatisticsSummaries { get; set; } = new List<IoTStatisticsSummary>();
    }
}
