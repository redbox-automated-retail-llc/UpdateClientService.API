using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Certificate.Security
{
    public interface IHashService
    {
        Task<string> GetKioskPassword(string kioskId);

        Task<string> GetCertificatePassword(string kioskId);
    }
}
