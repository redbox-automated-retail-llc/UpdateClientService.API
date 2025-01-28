using System.Collections.Generic;
using UpdateClientService.API.Services.FileSets;
using UpdateClientService.API.Services.IoT.DownloadFiles;
using UpdateClientService.API.Services.Kernel;

namespace UpdateClientService.API
{
    public class AppSettings
    {
        public string BaseServiceUrl { get; set; }

        public string UpdateClientInstallerUrl { get; set; }

        public int UpdateServiceInstanceCount { get; set; }

        public string IoTCertificateServiceUrl { get; set; }

        public string IoTCertificateServiceApiKey { get; set; }

        public string DefaultProxyServiceUrl { get; set; }

        public string IoTBrokerEndpoint { get; set; }

        public IEnumerable<CleanupFiles> CleanUpFiles { get; set; }

        public IEnumerable<string> FileVersionPaths { get; set; }

        public KernelServiceSettings KernelService { get; set; } = new KernelServiceSettings();

        public FileSetSettings FileSet { get; set; } = new FileSetSettings();

        public DownloadFilesSettings DownloadFiles { get; set; } = new DownloadFilesSettings();

        public ConfigurationSettings ConfigurationSettings { get; set; } = new ConfigurationSettings();

        public SyncTimeSettings SyncTime { get; set; } = new SyncTimeSettings();
    }
}
