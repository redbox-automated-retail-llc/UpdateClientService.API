using System.Threading.Tasks;
using UpdateClientService.API.Services.FileSets;

namespace UpdateClientService.API.Services.IoT.FileSets
{
    public interface IKioskFileSetVersionsService
    {
        Task<ReportFileSetVersionsResponse> ReportFileSetVersion(FileSetPollRequest version);

        Task<ReportFileSetVersionsResponse> ReportFileSetVersions();
    }
}
