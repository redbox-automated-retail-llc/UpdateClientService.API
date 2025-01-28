using MQTTnet.Client.Options;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace UpdateClientService.API.Services.IoT
{
    internal class RootCertificateTrust
    {
        private X509Certificate2Collection certificates;

        internal RootCertificateTrust() => this.certificates = new X509Certificate2Collection();

        internal void AddCert(X509Certificate2 x509Certificate2)
        {
            this.certificates.Add(x509Certificate2);
        }

        internal bool VerifyServerCertificate(MqttClientCertificateValidationCallbackContext arg)
        {
            return this.VerifyServerCertificate(new object(), arg.Certificate, arg.Chain, arg.SslPolicyErrors);
        }

        internal bool VerifyServerCertificate(
          object sender,
          X509Certificate certificate,
          X509Chain chain,
          SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            X509Chain x509Chain1 = new X509Chain();
            X509Chain x509Chain2 = chain;
            x509Chain2.ChainPolicy.ExtraStore.AddRange(this.certificates);
            x509Chain2.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            x509Chain2.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            if (x509Chain2.Build(new X509Certificate2(certificate)))
                return true;
            foreach (X509ChainStatus chainStatu in x509Chain2.ChainStatus)
            {
                if (chainStatu.Status != X509ChainStatusFlags.UntrustedRoot)
                    return false;
            }
            foreach (X509ChainElement chainElement in x509Chain2.ChainElements)
            {
                foreach (X509ChainStatus chainElementStatu in chainElement.ChainElementStatus)
                {
                    if (chainElementStatu.Status == X509ChainStatusFlags.UntrustedRoot && this.certificates.Find(X509FindType.FindByThumbprint, (object)chainElement.Certificate.Thumbprint, false).Count == 0)
                        return false;
                }
            }
            return true;
        }
    }
}
