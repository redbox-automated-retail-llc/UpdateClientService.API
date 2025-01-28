using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace UpdateClientService.API
{
    public static class StreamExtensions
    {
        public static async Task<string> GetSHA1HashAsync(this Stream stream)
        {
            string base64String;
            using (MemoryStream ms = new MemoryStream())
            {
                await stream.CopyToAsync((Stream)ms);
                byte[] array = ms.ToArray();
                using (SHA1Managed shA1Managed = new SHA1Managed())
                    base64String = Convert.ToBase64String(((HashAlgorithm)shA1Managed).ComputeHash(array));
            }
            return base64String;
        }

        public static string GetSHA1Hash(this Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo((Stream)memoryStream);
                byte[] array = memoryStream.ToArray();
                using (SHA1Managed shA1Managed = new SHA1Managed())
                    return Convert.ToBase64String(((HashAlgorithm)shA1Managed).ComputeHash(array));
            }
        }

        public static string ReadToEnd(this Stream stream)
        {
            using (StreamReader streamReader = new StreamReader(stream))
                return ((TextReader)streamReader).ReadToEnd();
        }

        public static byte[] GetBytes(this Stream stream)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo((Stream)memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
