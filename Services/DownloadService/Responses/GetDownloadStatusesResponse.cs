using Redbox.NetCore.Middleware.Http;
using System.Collections.Generic;

namespace UpdateClientService.API.Services.DownloadService.Responses
{
    public class GetDownloadStatusesResponse : ApiBaseResponse
    {
        public List<DownloadData> Statuses { get; set; }
    }
}
