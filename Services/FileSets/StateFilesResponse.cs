using Redbox.NetCore.Middleware.Http;
using System.Collections.Generic;

namespace UpdateClientService.API.Services.FileSets
{
    public class StateFilesResponse : ApiBaseResponse
    {
        public List<StateFile> StateFiles { get; set; }
    }
}
