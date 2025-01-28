using Redbox.NetCore.Middleware.Http;

namespace UpdateClientService.API.Services.DownloadService.Responses
{
    public class GetDownloadStatusResponse : ApiBaseResponse
    {
        public DownloadData Status { get; set; }
    }
}
