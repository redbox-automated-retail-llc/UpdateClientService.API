using System;
using UpdateClientService.API.App;

namespace UpdateClientService.API.Services.IoT.FileSets
{
    public class FileSetFileRevision : IJob, IPersistentData
    {
        public long Id => this.FileSetFileRevisionId;

        public long FileSetFileRevisionId { get; set; }

        public long FileSetFileId { get; set; }

        public Guid BlobId { get; set; }

        public string BlobHash { get; set; }

        public string Url { get; set; }
    }
}
