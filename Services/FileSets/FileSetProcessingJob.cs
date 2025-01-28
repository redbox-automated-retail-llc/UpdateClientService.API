using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetProcessingJob : IFileSetProcessingJob, IInvocable
    {
        private readonly IFileSetService _fileSetService;
        private readonly ILogger<FileSetProcessingJob> _logger;

        public FileSetProcessingJob(
          ILogger<FileSetProcessingJob> logger,
          IFileSetService fileSetService)
        {
            this._fileSetService = fileSetService;
            this._logger = logger;
        }

        public async Task Invoke()
        {
            try
            {
                await this._fileSetService.ProcessInProgressRevisionChangeSets();
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while running FileSetProcessingJob.", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetProcessingJob.cs");
            }
        }
    }
}
