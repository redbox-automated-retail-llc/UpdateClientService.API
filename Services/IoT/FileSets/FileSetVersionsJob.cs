using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System.Threading.Tasks;
using UpdateClientService.API.Services.FileSets;

namespace UpdateClientService.API.Services.IoT.FileSets
{
    public class FileSetVersionsJob : IFileSetVersionsJob, IInvocable
    {
        private readonly ILogger<FileSetVersionsJob> _logger;
        private readonly IFileSetService _fileSetService;

        public FileSetVersionsJob(ILogger<FileSetVersionsJob> logger, IFileSetService fileSetService)
        {
            this._logger = logger;
            this._fileSetService = fileSetService;
        }

        public async Task Invoke()
        {
            this._logger.LogInfoWithSource("ProcessPendingFileSetVersions", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/IoT/FileSets/FileSetVersionsJob.cs");
            ReportFileSetVersionsResponse versionsResponse = await this._fileSetService.ProcessPendingFileSetVersions();
        }
    }
}
