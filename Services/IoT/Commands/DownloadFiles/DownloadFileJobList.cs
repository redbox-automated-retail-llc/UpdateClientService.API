using System.Collections.Generic;

namespace UpdateClientService.API.Services.IoT.Commands.DownloadFiles
{
    public class DownloadFileJobList : IPersistentData
    {
        public List<DownloadFileJob> JobList { get; set; } = new List<DownloadFileJob>();
    }
}
