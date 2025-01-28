using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Certificate
{
    public interface ICertificateService
    {
        Task<IotCert> GetCertificateAsync(bool forceNew = false);

        Task<bool> Validate();
    }
}
