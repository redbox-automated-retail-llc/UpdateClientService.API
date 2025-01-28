namespace UpdateClientService.API.Services.FileSets
{
    public class ClientFileSetFile
    {
        public long FileId { get; set; }

        public string Name { get; set; }

        public string FileDestination { get; set; }

        public long FileRevisionId { get; set; }

        public string ContentHash { get; set; }

        public string Path { get; set; }

        public int CompressionType { get; set; }

        public string FileHash { get; set; }

        public string BlobId { get; set; }

        public long ContentSize { get; set; }

        public long FileSize { get; set; }
    }
}
