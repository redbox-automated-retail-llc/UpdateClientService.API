using Redbox.NetCore.Middleware.Http;

namespace UpdateClientService.API.Services.FileSets
{
    public class StateFileResponse : ApiBaseResponse
    {
        public StateFile StateFile { get; set; }
    }
}
