using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;

namespace UpdateClientService.API
{
    public class ApplicationWebHostService : WebHostService
    {
        private ILogger<ApplicationWebHostService> _logger;

        public ApplicationWebHostService(IWebHost host, ILogger<ApplicationWebHostService> logger)
          : base(host)
        {
            this._logger = logger;
        }

        protected override void OnStarting(string[] args)
        {
            ILogger<ApplicationWebHostService> logger = this._logger;
            if (logger != null)
                this._logger.LogInfoWithSource("WebHost: OnStarting", nameof(OnStarting), "/sln/src/UpdateClientService.API/ApplicationWebHostService.cs");
            base.OnStarting(args);
        }

        protected override void OnStarted()
        {
            ILogger<ApplicationWebHostService> logger = this._logger;
            if (logger != null)
                this._logger.LogInfoWithSource("WebHost: OnStarted", nameof(OnStarted), "/sln/src/UpdateClientService.API/ApplicationWebHostService.cs");
            base.OnStarted();
        }

        protected override void OnStopping()
        {
            ILogger<ApplicationWebHostService> logger = this._logger;
            if (logger != null)
                this._logger.LogInfoWithSource("WebHost: OnStopping", nameof(OnStopping), "/sln/src/UpdateClientService.API/ApplicationWebHostService.cs");
            base.OnStopping();
        }

        protected override void OnStopped()
        {
            ILogger<ApplicationWebHostService> logger = this._logger;
            if (logger != null)
                this._logger.LogInfoWithSource("WebHost: OnStopped", nameof(OnStopped), "/sln/src/UpdateClientService.API/ApplicationWebHostService.cs");
            base.OnStopped();
        }
    }
}
