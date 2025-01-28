using System.Collections.Generic;
using System.Threading.Tasks;
using UpdateClientService.API.Services.DownloadService;

namespace UpdateClientService.API.Services.FileSets
{
    public interface IChangeSetFileService
    {
        Task ProcessChangeSet(DownloadDataList downloads, RevisionChangeSet revisionChangeSet);

        Task ProcessActivationDependencyCheck(
          Dictionary<long, FileSetDependencyState> dependencyStates,
          RevisionChangeSet revisionChangeSet);

        Task ProcessActivationPending(RevisionChangeSet revisionChangeSet);

        Task ProcessActivationBeforeActions(RevisionChangeSet revisionChangeSet);

        Task ProcessActivating(RevisionChangeSet revisionChangeSet);

        Task ProcessActivationAfterActions(RevisionChangeSet revisionChangeSet);

        Task<bool> CreateRevisionChangeSet(ClientFileSetRevisionChangeSet set);

        Task<bool> Delete(RevisionChangeSet changeset);

        void CleanUp();

        Task<List<RevisionChangeSet>> GetAllRevisionChangeSets();
    }
}
