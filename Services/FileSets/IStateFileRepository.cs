using System.Collections.Generic;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.FileSets
{
    public interface IStateFileRepository
    {
        Task<(bool, List<StateFile>)> GetAll();

        Task<(bool, StateFile)> Get(long fileSetId);

        Task<bool> Save(StateFile stateFile);

        Task<bool> Delete(long fileSetId);

        Task<bool> DeleteInProgress(long fileSetId);
    }
}
