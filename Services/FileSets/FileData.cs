namespace UpdateClientService.API.Services.FileSets
{
    internal class FileData
    {
        public long FileId { get; set; }

        public long FileRevisionId { get; set; }

        public long Size { get; set; }

        public string Hash { get; set; }

        public string StagePath { get; set; }

        public string FileDestination { get; set; }
    }
}
