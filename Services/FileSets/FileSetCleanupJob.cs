using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetCleanupJob : IFileSetCleanupJob, IInvocable
    {
        private readonly IFileSetCleanup _fileSetCleanup;
        private readonly ILogger<FileSetCleanupJob> _logger;

        public FileSetCleanupJob(IFileSetCleanup fileSetCleanup, ILogger<FileSetCleanupJob> logger)
        {
            this._fileSetCleanup = fileSetCleanup;
            this._logger = logger;
        }

        public async Task Invoke()
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                this._logger.LogInfoWithSource("Starting FileSetCleanup", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetCleanupJob.cs");
                await this._fileSetCleanup.Run();
                this._logger.LogInfoWithSource(string.Format("Finished FileSetCleanup in {0} ms", (object)sw.ElapsedMilliseconds), nameof(Invoke), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetCleanupJob.cs");
                sw = (Stopwatch)null;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while running FileSet cleanup.", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/FileSets/FileSetCleanupJob.cs");
            }
        }
    }
}
