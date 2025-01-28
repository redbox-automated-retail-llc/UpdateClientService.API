using Redbox.NetCore.Middleware.Http;

namespace UpdateClientService.API.Services.DownloadService.Responses
{
    public class GetFileResponse : ApiBaseResponse
    {
        public string Key { get; set; }
    }
}
