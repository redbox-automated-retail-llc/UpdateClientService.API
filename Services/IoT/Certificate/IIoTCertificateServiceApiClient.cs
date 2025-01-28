using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Certificate
{
    public interface IIoTCertificateServiceApiClient
    {
        Task<IsCertificateValidResponse> IsCertificateValid(
          string kioskId,
          string thingType,
          string password,
          string certificateId);

        Task<IoTCertificateServiceResponse> GetCertificates(
          string kioskId,
          string thingType,
          string password);

        Task<IoTCertificateServiceResponse> GenerateNewCertificates(
          string kioskId,
          string thingType,
          string password);
    }
}
