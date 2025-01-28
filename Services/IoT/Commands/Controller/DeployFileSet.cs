using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using Redbox.NetCore.Middleware.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UpdateClientService.API.Services.FileSets;

namespace UpdateClientService.API.Services.IoT.Commands.Controller
{
    public class DeployFileSet : ICommandIoTController
    {
        private readonly ILogger<DeployFileSet> _logger;
        private readonly IFileSetService _service;

        public CommandEnum CommandEnum => CommandEnum.DeployFileSet;

        public int Version => 1;

        public DeployFileSet(ILogger<DeployFileSet> logger, IFileSetService service)
        {
            this._logger = logger;
            this._service = service;
        }

        public async Task Execute(IoTCommandModel ioTCommand)
        {
            try
            {
                List<Error> errorList = new List<Error>();
                ProcessChangeSetResponse changeSetResponse = await this._service.ProcessChangeSet(JsonConvert.DeserializeObject<ClientFileSetRevisionChangeSet>(ioTCommand.Payload.ToJson()));
            }
            catch (Exception ex)
            {
                ILogger<DeployFileSet> logger = this._logger;
                Exception exception = ex;
                IoTCommandModel ioTcommandModel = ioTCommand;
                string str = "Exception while executing command " + (ioTcommandModel != null ? ioTcommandModel.ToJson() : (string)null);
                this._logger.LogErrorWithSource(exception, str, nameof(Execute), "/sln/src/UpdateClientService.API/Services/IoT/Commands/Controller/DeployFileSet.cs");
            }
        }
    }
}
