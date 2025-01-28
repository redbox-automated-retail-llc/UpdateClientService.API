using System.Threading.Tasks;

namespace UpdateClientService.API.Services.FileSets
{
    public interface IStateFileService
    {
        Task<StateFilesResponse> GetAll();

        Task<StateFilesResponse> GetAllInProgress();

        Task<StateFileResponse> Get(long fileSetId);

        Task<StateFileResponse> Save(StateFile stateFile);

        Task<bool> Delete(long fileSetId);

        Task<bool> DeleteInProgress(long fileSetId);
    }
}
