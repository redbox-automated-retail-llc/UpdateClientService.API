using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.ServiceProcess;

namespace UpdateClientService.API.Services.Utilities
{
    public class WindowsServiceFunctions : IWindowsServiceFunctions
    {
        private readonly ICommandLineService _powerShellService;
        private readonly ILogger<WindowsServiceFunctions> _logger;

        public WindowsServiceFunctions(
          ICommandLineService powerShellService,
          ILogger<WindowsServiceFunctions> logger)
        {
            this._powerShellService = powerShellService;
            this._logger = logger;
        }

        public bool ServiceExists(string name)
        {
            foreach (ServiceController service in ServiceController.GetServices())
            {
                if (service.ServiceName == name)
                    return true;
            }
            return false;
        }

        public void StartService(string name)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(name))
                {
                    serviceController.Start();
                    serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMinutes(5.0));
                    this._logger.LogInfoWithSource(string.Format("Exiting from startService. Service name: {0} was {1}", (object)name, (object)serviceController.Status), nameof(StartService), "/sln/src/UpdateClientService.API/Services/Utilities/WindowsServiceFunctions.cs");
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "An unhandled exception was raised while starting service: " + name + ".", nameof(StartService), "/sln/src/UpdateClientService.API/Services/Utilities/WindowsServiceFunctions.cs");
            }
        }

        public void StopService(string name)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(name))
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMinutes(5.0));
                    this._logger.LogInfoWithSource(string.Format("Exiting from stopService. Service name: {0} was {1}", (object)name, (object)serviceController.Status), nameof(StopService), "/sln/src/UpdateClientService.API/Services/Utilities/WindowsServiceFunctions.cs");
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "An unhandled exception was raised while stopping service: " + name + ".", nameof(StopService), "/sln/src/UpdateClientService.API/Services/Utilities/WindowsServiceFunctions.cs");
            }
        }

        public void InstallService(
          string name,
          string displayName,
          string binPath,
          string type = null,
          string startType = null,
          string dependencies = null)
        {
            try
            {
                string arguments = string.Format("create {0} displayName= \"{1}\" binPath= \"{2}\" type= {3} start= {4}", (object)name, (object)displayName, (object)binPath, (object)(type ?? "own"), (object)(startType ?? "auto"));
                if (!string.IsNullOrEmpty(dependencies))
                    arguments += string.Format(" depend= {0}", (object)dependencies);
                this._powerShellService.TryExecuteCommand("SC.exe", arguments);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "An unhandled exception was raised while installing service " + name, nameof(InstallService), "/sln/src/UpdateClientService.API/Services/Utilities/WindowsServiceFunctions.cs");
            }
        }
    }
}
