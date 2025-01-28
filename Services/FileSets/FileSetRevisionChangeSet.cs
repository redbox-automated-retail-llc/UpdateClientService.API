namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetRevisionChangeSet
    {
        public long FileSetId { get; set; }

        public long RevisionId { get; set; }

        public string Path { get; set; }

        public int CompressionType { get; set; }

        public string ContentHash { get; set; }

        public string FileHash { get; set; }

        public long ContentSize { get; set; }

        public long FileSize { get; set; }

        public long PatchRevisionId { get; set; }

        public ClientFileSetRevisionChangeSet ToClient()
        {
            ClientFileSetRevisionChangeSet client = new ClientFileSetRevisionChangeSet();
            client.FileSetId = this.FileSetId;
            client.RevisionId = this.RevisionId;
            client.Path = this.Path;
            client.CompressionType = this.CompressionType;
            client.FileHash = this.FileHash;
            client.ContentHash = this.ContentHash;
            client.PatchRevisionId = this.PatchRevisionId;
            client.ContentSize = this.ContentSize;
            client.FileSize = this.FileSize;
            return client;
        }

        public FileSetRevisionChangeSet Clone()
        {
            return new FileSetRevisionChangeSet()
            {
                FileSetId = this.FileSetId,
                RevisionId = this.RevisionId,
                Path = this.Path,
                CompressionType = this.CompressionType,
                ContentHash = this.ContentHash,
                FileHash = this.FileHash,
                PatchRevisionId = this.PatchRevisionId,
                ContentSize = this.ContentSize,
                FileSize = this.FileSize
            };
        }
    }
}
