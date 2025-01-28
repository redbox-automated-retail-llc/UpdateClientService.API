using System;
using UpdateClientService.API.Services.DownloadService;

namespace UpdateClientService.API.Services.FileSets
{
    public class ClientFileSetRevisionChangeSet : RevisionChangeSetKey
    {
        public string Path { get; set; }

        public int CompressionType { get; set; }

        public string ContentHash { get; set; }

        public string FileHash { get; set; }

        public long ContentSize { get; set; }

        public long FileSize { get; set; }

        public DateTime DownloadOn { get; set; }

        public DownloadPriority DownloadPriority { get; set; }

        public DateTime ActiveOn { get; set; }

        public string ActivateStartTime { get; set; }

        public string ActivateEndTime { get; set; }

        public string DownloadUrl { get; set; }

        public FileSetAction Action { get; set; }
    }
}
