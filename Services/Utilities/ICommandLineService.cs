using System;

namespace UpdateClientService.API.Services.Utilities
{
    public interface ICommandLineService
    {
        bool TryExecuteCommand(string fileName, string arguments);

        bool TryExecutePowerShellScriptFromFile(string path);

        bool TryExecutePowerShellScript(string script, TimeSpan? timeout = null);
    }
}
