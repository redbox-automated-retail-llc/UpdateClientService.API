using Redbox.NetCore.Middleware.Http;

namespace UpdateClientService.API.Services.DataUpdate
{
    public class GetRecordChangesResponse : ApiBaseResponse
    {
        public string DataUpdateResponseFileName { get; set; }
    }
}
