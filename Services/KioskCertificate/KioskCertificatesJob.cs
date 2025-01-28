using Coravel.Invocable;
using Microsoft.Extensions.Logging;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.KioskCertificate
{
    internal class KioskCertificatesJob : IKioskCertificatesJob, IInvocable
    {
        private readonly string _certDataPath;
        private ILogger<KioskCertificatesJob> _logger;

        public KioskCertificatesJob(ILogger<KioskCertificatesJob> logger)
        {
            this._logger = logger;
            this._certDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Redbox\\UpdateClient\\Certificates\\");
        }

        public async Task Invoke()
        {
            try
            {
                await this.LookForCerts();
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "There was an exception in Invoke()", nameof(Invoke), "/sln/src/UpdateClientService.API/Services/KioskCertificate/KioskCertificatesJob.cs");
            }
        }

        private async Task LookForCerts()
        {
            if (Directory.Exists(this._certDataPath))
            {
                string[] files = Directory.GetFiles(this._certDataPath, "*.staged");
                if (files.Length == 0)
                    return;
                this._logger.LogInfoWithSource("Found following certs to process: " + string.Join(";", files), nameof(LookForCerts), "/sln/src/UpdateClientService.API/Services/KioskCertificate/KioskCertificatesJob.cs");
                string[] strArray = files;
                for (int index = 0; index < strArray.Length; ++index)
                {
                    string file = strArray[index];
                    try
                    {
                        byte[] data = File.ReadAllBytes(file);
                        if (!CertificateHelper.Exists(StoreName.My, StoreLocation.LocalMachine, data))
                        {
                            CertificateHelper.Add(StoreName.My, StoreLocation.LocalMachine, data);
                            this._logger.LogInfoWithSource("added cert: " + file + " to the capi store", nameof(LookForCerts), "/sln/src/UpdateClientService.API/Services/KioskCertificate/KioskCertificatesJob.cs");
                        }
                        else
                            this._logger.LogInfoWithSource("cert: " + file + " exists in capi store", nameof(LookForCerts), "/sln/src/UpdateClientService.API/Services/KioskCertificate/KioskCertificatesJob.cs");
                        string str = file.Replace(".staged", "");
                        if (File.Exists(str))
                            File.Delete(str);
                        File.Move(file, str);
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogErrorWithSource(ex, "exception occured processing certificate: " + file, nameof(LookForCerts), "/sln/src/UpdateClientService.API/Services/KioskCertificate/KioskCertificatesJob.cs");
                    }
                    file = (string)null;
                }
                strArray = (string[])null;
            }
            else
            {
                this._logger.LogInfoWithSource("creating certificate path: " + this._certDataPath, nameof(LookForCerts), "/sln/src/UpdateClientService.API/Services/KioskCertificate/KioskCertificatesJob.cs");
                Directory.CreateDirectory(this._certDataPath);
            }
        }
    }
}
