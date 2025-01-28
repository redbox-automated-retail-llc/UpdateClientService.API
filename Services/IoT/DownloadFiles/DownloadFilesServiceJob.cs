using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Redbox.NetCore.Logging.Extensions;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.DownloadFiles
{
    public class DownloadFilesServiceJob : IInvocable
    {
        private ILogger<DownloadFilesServiceJob> _logger;
        private IDownloadFilesService _downloadFilesService;
        private DownloadFilesSettings _settings;

        public DownloadFilesServiceJob(
          ILogger<DownloadFilesServiceJob> logger,
          IDownloadFilesService downloadFilesService,
          IOptionsMonitor<AppSettings> settings)
        {
            this._logger = logger;
            this._downloadFilesService = downloadFilesService;
            this._settings = settings.CurrentValue.DownloadFiles;
        }

        public async Task Invoke()
        {
            DownloadFilesSettings settings = this._settings;
            if ((settings != null ? (settings.Enabled ? 1 : 0) : 0) == 0)
                return;
            this._logger.LogInfoWithSource("HandleScheduledJobs", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/IoT/DownloadFiles/DownloadFilesServiceJob.cs");
            await this._downloadFilesService.HandleScheduledJobs();
        }
    }
}
