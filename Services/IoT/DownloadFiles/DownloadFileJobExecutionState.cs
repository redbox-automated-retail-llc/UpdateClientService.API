using System.Collections.Concurrent;

namespace UpdateClientService.API.Services.IoT.DownloadFiles
{
    public static class DownloadFileJobExecutionState
    {
        public static ConcurrentDictionary<string, bool> Executions = new ConcurrentDictionary<string, bool>();
    }
}
