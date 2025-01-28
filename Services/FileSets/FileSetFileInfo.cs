namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetFileInfo
    {
        public long FileId { get; set; }

        public long FileRevisionId { get; set; }

        public bool Exists { get; set; }

        public long PatchSize { get; set; }

        public long FileSize { get; set; }

        public string FileHash { get; set; }

        public string PatchHash { get; set; }

        public string Key { get; set; }

        public string PatchKey { get; set; }

        public string PatchUrl { get; set; }

        public string Url { get; set; }
    }
}
