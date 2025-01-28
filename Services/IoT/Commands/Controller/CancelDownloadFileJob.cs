using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Commands.DownloadFiles;
using UpdateClientService.API.Services.IoT.DownloadFiles;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class CancelDownloadFileJob : ICommandIoTController
    {
        private readonly IDownloadFilesService _downloadFilesService;
        private readonly IStoreService _store;
        private readonly ILogger<CancelDownloadFileJob> _logger;

        public CommandEnum CommandEnum => CommandEnum.CancelDownloadFileJob;

        public int Version => 1;

        public CancelDownloadFileJob(
          IDownloadFilesService downloadFilesService,
          IStoreService store,
          ILogger<CancelDownloadFileJob> logger)
        {
            this._downloadFilesService = downloadFilesService;
            this._store = store;
            this._logger = logger;
        }

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            try
            {
                DownloadFileJob job = JsonConvert.DeserializeObject<DownloadFileJob>(ioTCommand.Payload.ToString());
                if (!this.IsDownloadFileJobValid(job))
                    return;
                await this._downloadFilesService.CancelDownloadFileJob(job);
            }
            catch (Exception ex)
            {
                ILogger<CancelDownloadFileJob> logger = this._logger;
                Exception exception = ex;
                IoTCommandModel ioTcommandModel = ioTCommand;
                string str = "Exception while executing command " + (ioTcommandModel != null ? ioTcommandModel.ToJson() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/CancelDownloadFileJob.cs");
            }
        }

        private bool IsDownloadFileJobValid(DownloadFileJob job)
        {
            string downloadFileJobId = job.DownloadFileJobId;
            if (job == null)
            {
                this._logger.LogErrorWithSource("Job cannot be null", nameof(IsDownloadFileJobValid), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/CancelDownloadFileJob.cs");
                return false;
            }
            if (!job.TargetKiosks.Contains(this._store.KioskId))
                this._logger.LogErrorWithSource(string.Format("Job's target kiosks does not include {0}", (object)this._store.KioskId), nameof(IsDownloadFileJobValid), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/CancelDownloadFileJob.cs");
            if (string.IsNullOrWhiteSpace(downloadFileJobId))
            {
                this._logger.LogErrorWithSource("DownloadFileJobId cannot be null.", nameof(IsDownloadFileJobValid), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/CancelDownloadFileJob.cs");
                return false;
            }
            DownloadFileJobExecutionState.Executions.TryAdd(downloadFileJobId, true);
            return true;
        }
    }
}
