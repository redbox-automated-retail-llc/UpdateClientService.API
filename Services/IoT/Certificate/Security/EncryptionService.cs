using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UpdateClientService.API.Services.Configuration;

namespace UpdateClientService.API.Services.IoT.Certificate.Security
{
    public class EncryptionService : IEncryptionService
    {
        private readonly int _saltSize = 32;
        private readonly string _key = ",$=Aqy*)eChz+62ySJUTRX\\j5.hjeCO;8p*R+(90LQvCg?-K(}+at7'ns^IvL,CM+4;Dk3}Pt7@~ai(6u}Ub6Eg^tsl:KEB@&yX+,SK?:$6h[V4hwXEY#*|Oe5G9J6tmvNRDu*Gs4]lRzN4\\mzJkZ&?IOipWJZ6,DpXFh?t\"%LEb+At&V'Iwx|[w}R!.M`L6`{|q#u@o.@]16,v\\tmxe2\\\\[3o-UzFlEYV:We>5qq>eT7`]@c8$mVq;SELkHU3q\"R)x?XtFU\\B@<$qX;IE_'bSCsbf3ezF<'0<w}uQ(L0P*/x\"#:2<V![z0n'I;alt#8`<V)J];7__lNhD@?kD\\gzFI+GrmYsqT)\"`U[T(5/b$KKumUb+G+|>fe)IGQFaf^`X<`0ap-+cd_t{q8/weN6n/Jdqmu8*6EC7U{$[+3quaAiADOMz4k@d2yJ,Nv<pE=X`R^3.%WwZ|%)ge5[E@YBF1Eul9$w\"fm0Lu-7Jds{O?XDJ>'pUW[A";

        public async Task<string> Encrypt(ConfigurationEncryptionType encryptionType, string plainText)
        {
            if (encryptionType == ConfigurationEncryptionType.EncryptType1)
                return await this.Encrypt(plainText);
            throw new ArgumentException(string.Format("Invalid encryptionType value: {0}", (object)encryptionType));
        }

        public async Task<string> Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));
            if (string.IsNullOrEmpty(this._key))
                throw new ArgumentNullException("key");
            string base64String;
            using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(this._key, this._saltSize))
            {
                byte[] salt = rfc2898DeriveBytes.Salt;
                byte[] bytes1 = rfc2898DeriveBytes.GetBytes(32);
                byte[] bytes2 = rfc2898DeriveBytes.GetBytes(16);
                using (AesManaged aesManaged = new AesManaged())
                {
                    using (ICryptoTransform encryptor = ((SymmetricAlgorithm)aesManaged).CreateEncryptor(bytes1, bytes2))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                                    ((TextWriter)streamWriter).Write(plainText);
                            }
                            byte[] array = memoryStream.ToArray();
                            Array.Resize<byte>(ref salt, salt.Length + array.Length);
                            Array.Copy((Array)array, 0, (Array)salt, this._saltSize, array.Length);
                            base64String = Convert.ToBase64String(salt);
                        }
                    }
                }
            }
            return base64String;
        }

        public async Task<string> Decrypt(ConfigurationEncryptionType encryptionType, string cipherText)
        {
            if (encryptionType == ConfigurationEncryptionType.EncryptType1)
                return await this.Decrypt(cipherText);
            throw new ArgumentException(string.Format("Invalid encryptionType value: {0}", (object)encryptionType));
        }

        public async Task<string> Decrypt(string ciphertext)
        {
            if (string.IsNullOrEmpty(ciphertext))
                throw new ArgumentNullException("cipherText");
            if (string.IsNullOrEmpty(this._key))
                throw new ArgumentNullException("key");
            byte[] source = Convert.FromBase64String(ciphertext);
            byte[] array1 = ((IEnumerable<byte>)source).Take<byte>(this._saltSize).ToArray<byte>();
            byte[] array2 = ((IEnumerable<byte>)source).Skip<byte>(this._saltSize).Take<byte>(source.Length - this._saltSize).ToArray<byte>();
            string endAsync;
            using (Rfc2898DeriveBytes keyDerivationFunction = new Rfc2898DeriveBytes(this._key, array1))
            {
                byte[] bytes1 = keyDerivationFunction.GetBytes(32);
                byte[] bytes2 = keyDerivationFunction.GetBytes(16);
                using (AesManaged aesManaged = new AesManaged())
                {
                    using (ICryptoTransform decryptor = ((SymmetricAlgorithm)aesManaged).CreateDecryptor(bytes1, bytes2))
                    {
                        using (MemoryStream memoryStream = new MemoryStream(array2))
                        {
                            using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                            {
                                using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                                    endAsync = await ((TextReader)streamReader).ReadToEndAsync();
                            }
                        }
                    }
                }
            }
            return endAsync;
        }
    }
}
