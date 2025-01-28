using Redbox.NetCore.Middleware.Http;
using System.Collections.Generic;
using System.Net;

namespace UpdateClientService.API.Services
{
    public class FileVersionDataResponse : ApiBaseResponse
    {
        public FileVersionDataResponse() => this.StatusCode = HttpStatusCode.OK;

        public IEnumerable<FileVersionData> Data { get; set; }
    }
}
