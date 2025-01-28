using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Threading.Tasks;
using UpdateClientService.API.Services.FileSets;
using UpdateClientService.API.Services.IoT.FileSets;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class TriggerReportFileSetVersions : ICommandIoTController
    {
        private readonly IFileSetService _fileSetService;
        private readonly ILogger<TriggerReportFileSetVersions> _logger;

        public TriggerReportFileSetVersions(
          IFileSetService fileSetService,
          ILogger<TriggerReportFileSetVersions> logger)
        {
            this._fileSetService = fileSetService;
            this._logger = logger;
        }

        public CommandEnum CommandEnum { get; } = CommandEnum.TriggerReportFileSetVersions;

        public int Version { get; } = 1;

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            TriggerReportFileSetVersionsRequest triggerReportFileSetVersionsRequest = JsonConvert.DeserializeObject<TriggerReportFileSetVersionsRequest>(ioTCommand.Payload.ToJson());
            try
            {
                ReportFileSetVersionsResponse versionsResponse = await this._fileSetService.TriggerProcessPendingFileSetVersions(triggerReportFileSetVersionsRequest);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception trying to trigger ReportFileSetVersions", nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/TriggerReportFileSetVersions.cs");
            }
        }
    }
}
