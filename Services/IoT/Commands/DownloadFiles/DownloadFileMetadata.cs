namespace UpdateClientService.API.Services.IoT.Commands.DownloadFiles
{
    public class DownloadFileMetadata
    {
        public string FileKey { get; set; }

        public string Name { get; set; }

        public string FileName { get; set; }

        public string DestinationPath { get; set; }

        public string ActivationScriptName { get; set; }
    }
}
