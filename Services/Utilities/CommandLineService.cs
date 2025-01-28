using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.Diagnostics;
using System.IO;

namespace UpdateClientService.API.Services.Utilities
{
    public class CommandLineService : ICommandLineService
    {
        private readonly ILogger<CommandLineService> _logger;

        public CommandLineService(ILogger<CommandLineService> logger) => this._logger = logger;

        public bool TryExecutePowerShellScriptFromFile(string path)
        {
            return this.Execute(new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = (ProcessWindowStyle)1,
                    FileName = "powershell",
                    Arguments = "-executionpolicy bypass -file \"" + path + "\""
                }
            });
        }

        public bool TryExecutePowerShellScript(string script, TimeSpan? timeout = null)
        {
            return this.Execute(new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = (ProcessWindowStyle)1,
                    FileName = "powershell",
                    Arguments = script
                }
            }, timeout);
        }

        public bool TryExecuteCommand(string fileName, string arguments)
        {
            return this.Execute(new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = (ProcessWindowStyle)1,
                    FileName = fileName,
                    Arguments = arguments
                }
            });
        }

        private bool Execute(Process process, TimeSpan? timeout = null)
        {
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            string end1 = ((TextReader)process.StandardOutput).ReadToEnd();
            string end2 = ((TextReader)process.StandardError).ReadToEnd();
            if (!process.WaitForExit(timeout.HasValue ? (int)timeout.GetValueOrDefault().TotalMilliseconds : (int)TimeSpan.FromMinutes(5.0).TotalMilliseconds))
                end2 += string.Format("Process did not complete within {0}.", (object)timeout);
            if (!string.IsNullOrWhiteSpace(end1))
                this._logger.LogInfoWithSource(end1, nameof(Execute), "/sln/src/UpdateClientService.API/Services/Utilities/CommandLineService.cs");
            if (!string.IsNullOrWhiteSpace(end2))
                this._logger.LogErrorWithSource("An error occurred while running a command. Error: " + end2, nameof(Execute), "/sln/src/UpdateClientService.API/Services/Utilities/CommandLineService.cs");
            return string.IsNullOrWhiteSpace(end2);
        }
    }
}
