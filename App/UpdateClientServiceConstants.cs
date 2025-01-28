using System;
using System.IO;

namespace UpdateClientService.API.App
{
    public class UpdateClientServiceConstants
    {
        public const string S3ProxyEndpoint = "api/downloads/s3/proxy";
        public const string UpdateClientWindowsServiceName = "updateclient$service";
        public const string AppName = "UpdateServiceClient";
        public const string UCSRegisterRuleTopic = "$aws/rules/kioskucsregister";
        public const string UCSPingRuleTopic = "$aws/rules/kioskping";
        public const string KioskRestCallTopic = "$aws/rules/kioskrestcall";
        public const string Version = "2.0";

        public static string DownloadDataFolder
        {
            get
            {
                return Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "/Redbox/UpdateClient/DownloadData").FullName;
            }
        }
    }
}
