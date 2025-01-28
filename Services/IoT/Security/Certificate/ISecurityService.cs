using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Security.Certificate
{
    public interface ISecurityService
    {
        Task<string> GetIoTCertServicePassword(string kioskId);

        Task<string> GetCertificatePassword(string kioskId);

        Task<string> Encrypt(string plainText);
    }
}
