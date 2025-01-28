using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.FileSets
{
    public interface IRevisionChangeSetRepository
    {
        Task<List<RevisionChangeSet>> GetAll();

        Task<bool> Save(RevisionChangeSet revisionChangeSet);

        Task<bool> Delete(RevisionChangeSet revisionChangeSet);

        Task<bool> Cleanup(DateTime deleteDate);
    }
}
