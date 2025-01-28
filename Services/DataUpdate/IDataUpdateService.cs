using System.Threading.Tasks;

namespace UpdateClientService.API.Services.DataUpdate
{
    public interface IDataUpdateService
    {
        Task<GetRecordChangesResponse> GetRecordChanges(DataUpdateRequest dataUpdateRequest);
    }
}
