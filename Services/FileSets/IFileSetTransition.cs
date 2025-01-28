using System.Collections.Generic;

namespace UpdateClientService.API.Services.FileSets
{
    public interface IFileSetTransition
    {
        bool RebootRequired { get; }

        void MarkRebootRequired();

        void ClearRebootRequired();

        bool Stage(ClientFileSetRevision clientFileSetRevision);

        bool CheckMeetsDependency(
          ClientFileSetRevision clientFileSetRevision,
          Dictionary<long, FileSetDependencyState> dependencyStates);

        bool BeforeActivate(ClientFileSetRevision clientFileSetRevision);

        bool AfterActivate(ClientFileSetRevision clientFileSetRevision);

        bool Activate(ClientFileSetRevision clientFileSetRevision);
    }
}
