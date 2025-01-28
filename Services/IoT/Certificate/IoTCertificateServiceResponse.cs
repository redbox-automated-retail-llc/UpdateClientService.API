namespace UpdateClientService.API.Services.IoT.Certificate
{
    public class IoTCertificateServiceResponse
    {
        public string DeviceCertPfxBase64 { get; set; }

        public string RootCa { get; set; }

        public string CertificateId { get; set; }
    }
}
