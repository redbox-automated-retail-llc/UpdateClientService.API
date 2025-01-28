using System;
using System.Security.Cryptography;

namespace UpdateClientService.API.App
{
    public static class ByteExtensions
    {
        public static string GetSHA1Hash(this byte[] data)
        {
            using (SHA1Managed shA1Managed = new SHA1Managed())
                return Convert.ToBase64String(((HashAlgorithm)shA1Managed).ComputeHash(data));
        }
    }
}
