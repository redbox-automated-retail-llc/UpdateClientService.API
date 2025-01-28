using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.FileSets;

namespace UpdateClientService.API.Services.FileSets
{
    public interface IFileSetService
    {
        Task<ProcessChangeSetResponse> ProcessChangeSet(
          ClientFileSetRevisionChangeSet clientFileSetRevisionChangeSet);

        Task<ReportFileSetVersionsResponse> ProcessPendingFileSetVersions();

        Task<ReportFileSetVersionsResponse> TriggerProcessPendingFileSetVersions(
          TriggerReportFileSetVersionsRequest triggerReportFileSetVersionsRequest);

        Task ProcessInProgressRevisionChangeSets();
    }
}
