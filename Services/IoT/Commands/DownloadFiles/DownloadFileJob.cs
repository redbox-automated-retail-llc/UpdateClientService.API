using System;
using System.Collections.Generic;

namespace UpdateClientService.API.Services.IoT.Commands.DownloadFiles
{
    public class DownloadFileJob
    {
        public string DownloadFileJobId { get; set; }

        public string FileKey { get; set; }

        public DownloadFileMetadata Metadata { get; set; }

        public DateTimeOffset StartDateUtc { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public DateTimeOffset CreatedOnUtc { get; set; }

        public string PresignedFileUrl { get; set; }

        public string PresignedScriptUrl { get; set; }

        public List<long> TargetKiosks { get; set; }

        public Guid BitsJobId { get; set; }
    }
}
