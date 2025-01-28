using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Commands.DownloadFiles;
using UpdateClientService.API.Services.IoT.DownloadFiles;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class ExecuteDownloadFileJob : ICommandIoTController
    {
        private readonly IDownloadFilesService _downloadFilesService;
        private readonly IStoreService _store;
        private readonly ILogger<ExecuteDownloadFileJob> _logger;

        public CommandEnum CommandEnum => CommandEnum.ExecuteDownloadFileJob;

        public int Version => 1;

        public ExecuteDownloadFileJob(
          IDownloadFilesService downloadFilesService,
          IStoreService store,
          ILogger<ExecuteDownloadFileJob> logger)
        {
            this._downloadFilesService = downloadFilesService;
            this._store = store;
            this._logger = logger;
        }

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            try
            {
                DownloadFileJob requestModel = JsonConvert.DeserializeObject<DownloadFileJob>(ioTCommand.Payload.ToString());
                if (this.IsDownloadFileJobValid(requestModel))
                {
                    await this._downloadFilesService.HandleDownloadFileJob(requestModel);
                    DownloadFileJobExecutionState.Executions.TryRemove(requestModel.DownloadFileJobId, out bool _);
                }
                requestModel = (DownloadFileJob)null;
            }
            catch (Exception ex)
            {
                ILogger<ExecuteDownloadFileJob> logger = this._logger;
                Exception exception = ex;
                IoTCommandModel ioTcommandModel = ioTCommand;
                string str = "Exception while executing command " + (ioTcommandModel != null ? ioTcommandModel.ToJson() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/ExecuteDownloadFileJob.cs");
            }
        }

        private bool IsDownloadFileJobValid(DownloadFileJob job)
        {
            string downloadFileJobId = job.DownloadFileJobId;
            if (job == null)
            {
                this._logger.LogErrorWithSource("Job cannot be null", nameof(IsDownloadFileJobValid), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/ExecuteDownloadFileJob.cs");
                return false;
            }
            if (!job.TargetKiosks.Contains(this._store.KioskId))
                this._logger.LogErrorWithSource(string.Format("Job's target kiosks does not include {0}", (object)this._store.KioskId), nameof(IsDownloadFileJobValid), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/ExecuteDownloadFileJob.cs");
            if (string.IsNullOrWhiteSpace(downloadFileJobId))
            {
                this._logger.LogErrorWithSource("DownloadFileJobId cannot be null.", nameof(IsDownloadFileJobValid), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/ExecuteDownloadFileJob.cs");
                return false;
            }
            if (DownloadFileJobExecutionState.Executions.TryAdd(downloadFileJobId, true))
                return true;
            this._logger.LogInfoWithSource("Another job with DownloadFileJobId " + downloadFileJobId + " is already in progress. Skipping...", nameof(IsDownloadFileJobValid), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/ExecuteDownloadFileJob.cs");
            return false;
        }
    }
}
