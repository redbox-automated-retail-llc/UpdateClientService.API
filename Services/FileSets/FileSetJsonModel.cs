namespace UpdateClientService.API.Services.FileSets
{
    public class FileSetJsonModel
    {
        public string ReleaseName { get; set; }

        public string InstallPath { get; set; }

        public string WindowsServiceName { get; set; }

        public string WindowsServiceDisplayName { get; set; }

        public string WindowsServiceStartFile { get; set; }
    }
}
