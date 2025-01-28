using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UpdateClientService.API.Services.IoT.Certificate.Security
{
    public class HashService : IHashService
    {
        public async Task<string> GetKioskPassword(string kioskId)
        {
            SHA512 shA512 = (SHA512)new SHA512Managed();
            string base64String = Convert.ToBase64String(shA512.ComputeHash(Encoding.UTF8.GetBytes(kioskId)));
            Task<string> theKioskPassword = this.GetSaltOfTheKioskPassword(kioskId);
            return Convert.ToBase64String(shA512.ComputeHash(Encoding.UTF8.GetBytes(base64String + (object)theKioskPassword)));
        }

        public async Task<string> GetCertificatePassword(string kioskId)
        {
            SHA512 shA512 = (SHA512)new SHA512Managed();
            string base64String = Convert.ToBase64String(shA512.ComputeHash(Encoding.UTF8.GetBytes(kioskId)));
            Task<string> certificatePassword = this.GetSaltOfTheCertificatePassword(kioskId);
            return Convert.ToBase64String(shA512.ComputeHash(Encoding.UTF8.GetBytes(base64String + (object)certificatePassword)));
        }

        private async Task<string> GetSaltOfTheCertificatePassword(string kioskId)
        {
            int num = 0;
            char ch1 = kioskId[kioskId.Length - 1];
            foreach (char ch2 in kioskId + kioskId)
                num *= (int)ch2 * (int)ch1;
            return num.ToString();
        }

        private async Task<string> GetSaltOfTheKioskPassword(string kioskId)
        {
            int num = 0;
            foreach (char ch in kioskId)
                num *= (int)ch;
            return num.ToString();
        }
    }
}
