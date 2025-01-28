using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace UpdateClientService.API.Services.IoT.Certificate
{
    public class IotCert
    {
        public X509Certificate RootCa { get; private set; }

        public X509Certificate2 DeviceCertPfx { get; private set; }

        public string CertificateId { get; private set; }

        public IotCert(
          string deviceCertPfxBase64,
          string rootCa,
          string certificateId,
          string pfxCertPassword)
        {
            this.RootCa = new X509Certificate(Encoding.ASCII.GetBytes(rootCa));
            this.DeviceCertPfx = new X509Certificate2(Convert.FromBase64String(deviceCertPfxBase64), pfxCertPassword);
            this.CertificateId = certificateId;
        }
    }
}
