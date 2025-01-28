using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Redbox.NetCore.Logging.Extensions;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UpdateClientService.API.Services.IoT.Security.Certificate;

namespace UpdateClientService.API.Services.IoT.Certificate
{
    internal class CertificateService : ICertificateService
    {
        private readonly ILogger<CertificateService> _logger;
        private readonly IIoTCertificateServiceApiClient _iotCertApiClient;
        private readonly IStoreService _store;
        private readonly ISecurityService _securityService;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly int _lockWait = 2000;
        private string _encryptedThingType;
        private string _encryptedKioskId;
        private long _kioskId;
        private string _ioTCertificateServicePassword;
        private const string IoTCertificateDataFileName = "iotcertificatedata.json";
        private const string ThingType = "kiosks";
        private string _ioTCertificateDataFilePath;

        private string IoTCertificateDataFilePath
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(this._ioTCertificateDataFilePath))
                    return this._ioTCertificateDataFilePath;
                string str = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData, Environment.SpecialFolderOption.Create), "Redbox\\UpdateClient\\IoT");
                Directory.CreateDirectory(str);
                this._ioTCertificateDataFilePath = Path.GetFullPath(Path.Combine(str, "iotcertificatedata.json"));
                return this._ioTCertificateDataFilePath;
            }
        }

        public CertificateService(
          ILogger<CertificateService> logger,
          IIoTCertificateServiceApiClient iotCertApiClient,
          IStoreService store,
          ISecurityService securityService)
        {
            this._logger = logger;
            this._iotCertApiClient = iotCertApiClient;
            this._store = store;
            this._securityService = securityService;
        }

        public async Task<IotCert> GetCertificateAsync(bool forceNew = false)
        {
            this._logger.LogInfoWithSource(string.Format("force = {0}", (object)forceNew), nameof(GetCertificateAsync), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            try
            {
                if (this._store.KioskId <= 0L)
                {
                    this._logger.LogErrorWithSource(string.Format("KioskId: {0} is not valid, no cert to return", (object)this._store.KioskId), nameof(GetCertificateAsync), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                    return (IotCert)null;
                }
                if (!await this.UpdateIotData())
                {
                    this._logger.LogErrorWithSource("IotData failed so we couldn't get certificate", nameof(GetCertificateAsync), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                    return (IotCert)null;
                }
                bool flag = this.IsExecutingAssemblyNewerThanCertificate();
                if (flag)
                    this._logger.LogInfoWithSource("Executing Assembly is newer than IoT Certificate. Forcing creation of new certificate.", nameof(GetCertificateAsync), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                if (forceNew | flag)
                    return !await this.WriteIoTCertificateDataFileAsync(await this._iotCertApiClient.GenerateNewCertificates(this._encryptedKioskId, this._encryptedThingType, this._ioTCertificateServicePassword)) ? (IotCert)null : await this.LoadCertificate();
                IotCert certificateAsync = await this.LoadCertificate();
                if (certificateAsync != null)
                    return certificateAsync;
                return !await this.WriteIoTCertificateDataFileAsync(await this._iotCertApiClient.GetCertificates(this._encryptedKioskId, this._encryptedThingType, this._ioTCertificateServicePassword)) ? (IotCert)null : await this.LoadCertificate();
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception running GetCertificateAsync", nameof(GetCertificateAsync), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                return (IotCert)null;
            }
        }

        public async Task<bool> Validate()
        {
            this._logger.LogInfoWithSource("Attempting to run Validate", nameof(Validate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            try
            {
                if (!await this.UpdateIotData())
                {
                    this._logger.LogErrorWithSource("Validate failed because IotData was not valid", nameof(Validate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                    return false;
                }
                if (await this.IsCertificateValid() ?? true)
                    return true;
                await this.DeleteLocalCertificate();
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception running validate", nameof(Validate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            }
            return false;
        }

        private bool IsExecutingAssemblyNewerThanCertificate()
        {
            bool flag = false;
            this._logger.LogInfoWithSource("Attempting to check if Executing Assembly is newer than cert", nameof(IsExecutingAssemblyNewerThanCertificate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            try
            {
                if (File.Exists(this.IoTCertificateDataFilePath))
                {
                    DateTime lastWriteTimeUtc1 = File.GetLastWriteTimeUtc(this.IoTCertificateDataFilePath);
                    DateTime lastWriteTimeUtc2 = File.GetLastWriteTimeUtc(Assembly.GetExecutingAssembly().Location);
                    flag = lastWriteTimeUtc2 > lastWriteTimeUtc1;
                    if (flag)
                        this._logger.LogInfoWithSource(string.Format("Assembly date {0} is newer than certificate date {1}", (object)lastWriteTimeUtc2, (object)lastWriteTimeUtc1), nameof(IsExecutingAssemblyNewerThanCertificate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                }
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception while comparing certificate date with assembly date.", nameof(IsExecutingAssemblyNewerThanCertificate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            }
            return flag;
        }

        private async Task<bool> UpdateIotData()
        {
            this._logger.LogInfoWithSource("Running UpdateIotData", nameof(UpdateIotData), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            try
            {
                long kioskId = this._store.KioskId;
                if (kioskId <= 0L)
                {
                    this._logger.LogErrorWithSource(string.Format("KioskId: {0} is not valid", (object)this._store.KioskId), nameof(UpdateIotData), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                    this._kioskId = 0L;
                }
                if (kioskId == this._kioskId && !string.IsNullOrEmpty(this._encryptedKioskId) && !string.IsNullOrEmpty(this._ioTCertificateServicePassword) && !string.IsNullOrEmpty(this._encryptedThingType))
                {
                    this._logger.LogInfoWithSource("IotData is already loaded", nameof(UpdateIotData), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                    return true;
                }
                this._kioskId = kioskId;
                this._encryptedKioskId = await this._securityService.Encrypt(this._store.KioskId.ToString());
                this._encryptedThingType = await this._securityService.Encrypt("kiosks");
                this._ioTCertificateServicePassword = await this._securityService.GetIoTCertServicePassword(this._store.KioskId.ToString());
                this._logger.LogInfoWithSource("Finished loading Iot data", nameof(UpdateIotData), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception occured loading IotData", nameof(UpdateIotData), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                return false;
            }
        }

        private void Clear()
        {
            this._encryptedKioskId = (string)null;
            this._encryptedThingType = (string)null;
            this._ioTCertificateServicePassword = (string)null;
        }

        private async Task<bool?> IsCertificateValid()
        {
            this._logger.LogInfoWithSource("Attempting to run IsCertificateValid", nameof(IsCertificateValid), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            try
            {
                IotCert iotCert = await this.LoadCertificate();
                if (iotCert == null)
                {
                    this._logger.LogInfoWithSource("iIotCert is null, cert is not valid", nameof(IsCertificateValid), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                    return new bool?(false);
                }
                IsCertificateValidResponse certificateValidResponse = await this._iotCertApiClient.IsCertificateValid(this._encryptedKioskId, this._encryptedThingType, this._ioTCertificateServicePassword, await this._securityService.Encrypt(iotCert.CertificateId));
                bool? isValid = certificateValidResponse.IsValid;
                if (!isValid.HasValue)
                {
                    this._logger.LogInfoWithSource("iotCertApiClient.IsCertificateValid was null, call failed can't tell if valid or not", nameof(IsCertificateValid), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                    return new bool?();
                }
                isValid = certificateValidResponse.IsValid;
                if (isValid.Value)
                {
                    this._logger.LogInfoWithSource("iotCertApiClient.IsCertificateValid was true, cert is valid", nameof(IsCertificateValid), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                    return new bool?(true);
                }
                this._logger.LogInfoWithSource("iotCertApiClient.IsCertificateValid was false, cert is not valid", nameof(IsCertificateValid), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                return new bool?(false);
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception occured checking to see if the certificate is valid", nameof(IsCertificateValid), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                return new bool?();
            }
        }

        private async Task DeleteLocalCertificate()
        {
            this._logger.LogInfoWithSource("Deleting the local certificate file: " + this.IoTCertificateDataFilePath, nameof(DeleteLocalCertificate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            if (!await this._lock.WaitAsync(this._lockWait))
            {
                this._logger.LogErrorWithSource("Lock failed, certificate not deleted", nameof(DeleteLocalCertificate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            }
            else
            {
                try
                {
                    this.Clear();
                    File.Delete(this.IoTCertificateDataFilePath);
                }
                catch (Exception ex)
                {
                    this._logger.LogErrorWithSource(ex, "Exception occured deleting certificate file", nameof(DeleteLocalCertificate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                }
                finally
                {
                    this._lock.Release();
                }
            }
        }

        private async Task<IotCert> LoadCertificate()
        {
            this._logger.LogInfoWithSource("Attempting to load Certificate", nameof(LoadCertificate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            if (!await this._lock.WaitAsync(this._lockWait))
            {
                this._logger.LogErrorWithSource("Lock failed, certificate cannot be loaded", nameof(LoadCertificate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                return (IotCert)null;
            }
            try
            {
                if (!File.Exists(this.IoTCertificateDataFilePath))
                {
                    this._logger.LogErrorWithSource("Certificate file does not exist: " + this.IoTCertificateDataFilePath, nameof(LoadCertificate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                    return (IotCert)null;
                }
                IoTCertificateServiceResponse localCerts = JsonConvert.DeserializeObject<IoTCertificateServiceResponse>(File.ReadAllText(this.IoTCertificateDataFilePath));
                return new IotCert(localCerts.DeviceCertPfxBase64, localCerts.RootCa, localCerts.CertificateId, await this._securityService.GetCertificatePassword(this._store.KioskId.ToString()));
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Exception loading certificate", nameof(LoadCertificate), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                return (IotCert)null;
            }
            finally
            {
                this._lock.Release();
            }
        }

        private async Task<bool> WriteIoTCertificateDataFileAsync(IoTCertificateServiceResponse response)
        {
            if (response == null)
            {
                this._logger.LogInfoWithSource("Response was null or not successful, skipping writing certificates", nameof(WriteIoTCertificateDataFileAsync), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                await this.DeleteLocalCertificate();
                return false;
            }
            this._logger.LogInfoWithSource("Attempting to write certificate to a file", nameof(WriteIoTCertificateDataFileAsync), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
            if (!await this._lock.WaitAsync(this._lockWait))
            {
                this._logger.LogErrorWithSource("Lock failed, certificate cannot be saved", nameof(WriteIoTCertificateDataFileAsync), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                return false;
            }
            try
            {
                File.WriteAllText(this.IoTCertificateDataFilePath, response.ToJson());
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogErrorWithSource(ex, "Response: " + response.ToJson(), nameof(WriteIoTCertificateDataFileAsync), "/sln/src/UpdateClientService.API/Services/IoT/Certificate/CertificateService.cs");
                return false;
            }
            finally
            {
                this._lock.Release();
            }
        }
    }
}
