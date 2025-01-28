using System.Security.Cryptography.X509Certificates;

namespace UpdateClientService.API.Services.KioskCertificate
{
    internal class CertificateHelper
    {
        public static X509Certificate2 GetCertificateFromFile(string fileName)
        {
            return new X509Certificate2(fileName);
        }

        public static X509Certificate2 GetCertificateFromFile(string fileName, string password)
        {
            return new X509Certificate2(fileName, password);
        }

        public static X509Certificate2 GetCertificateBySubjectName(
          StoreName storeName,
          StoreLocation storeLocation,
          string subjectName)
        {
            if (string.IsNullOrEmpty(subjectName))
                return (X509Certificate2)null;
            X509Store x509Store = new X509Store(storeName, storeLocation);
            try
            {
                x509Store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certificate2Collection = x509Store.Certificates.Find(X509FindType.FindBySubjectName, (object)subjectName, false);
                return certificate2Collection.Count > 0 ? certificate2Collection[0] : (X509Certificate2)null;
            }
            finally
            {
                x509Store.Close();
            }
        }

        public static bool Add(StoreName storeName, StoreLocation storeLocation, byte[] data)
        {
            X509Store x509Store = new X509Store(storeName, storeLocation);
            try
            {
                x509Store.Open(OpenFlags.MaxAllowed);
                X509Certificate2 certificate = new X509Certificate2(data);
                x509Store.Add(certificate);
            }
            finally
            {
                x509Store.Close();
            }
            return true;
        }

        public static bool Exists(StoreName storeName, StoreLocation storeLocation, byte[] data)
        {
            X509Certificate2 x509Certificate2 = new X509Certificate2(data);
            return CertificateHelper.GetCertificateByThumbPrint(storeName, storeLocation, x509Certificate2.Thumbprint) != null;
        }

        public static X509Certificate2 GetCertificateByThumbPrint(
          StoreName storeName,
          StoreLocation storeLocation,
          string thumbPrint)
        {
            X509Store x509Store = new X509Store(storeName, storeLocation);
            try
            {
                x509Store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certificate2Collection = x509Store.Certificates.Find(X509FindType.FindByThumbprint, (object)thumbPrint, false);
                return certificate2Collection.Count > 0 ? certificate2Collection[0] : (X509Certificate2)null;
            }
            finally
            {
                x509Store.Close();
            }
        }

        public static bool SubjectDistinguishedNameExists(
          StoreName storeName,
          StoreLocation storeLocation,
          byte[] data)
        {
            X509Certificate2 x509Certificate2 = new X509Certificate2(data);
            return CertificateHelper.GetCertificateBySubjectDistinguishedName(storeName, storeLocation, x509Certificate2.SubjectName) != null;
        }

        public static X509Certificate2 GetCertificateBySubjectDistinguishedName(
          StoreName storeName,
          StoreLocation storeLocation,
          X500DistinguishedName subjectName)
        {
            if (subjectName == null)
                return (X509Certificate2)null;
            X509Store x509Store = new X509Store(storeName, storeLocation);
            try
            {
                x509Store.Open(OpenFlags.ReadOnly);
                string findValue = subjectName.Name.ToString();
                X509Certificate2Collection certificate2Collection = x509Store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, (object)findValue, false);
                return certificate2Collection.Count > 0 ? certificate2Collection[0] : (X509Certificate2)null;
            }
            finally
            {
                x509Store.Close();
            }
        }
    }
}
