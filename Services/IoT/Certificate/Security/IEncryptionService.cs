using System.Threading.Tasks;
using UpdateClientService.API.Services.Configuration;

namespace UpdateClientService.API.Services.IoT.Certificate.Security
{
    public interface IEncryptionService
    {
        Task<string> Encrypt(string plainText);

        Task<string> Encrypt(ConfigurationEncryptionType encryptionType, string plainText);

        Task<string> Decrypt(string ciphertext);

        Task<string> Decrypt(ConfigurationEncryptionType encryptionType, string cipherText);
    }
}
