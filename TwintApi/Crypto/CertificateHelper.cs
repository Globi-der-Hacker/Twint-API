using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;

namespace TwintApi.Crypto
{
    public static class CertificateHelper
    {
        private const string CaCertSubjectName = "C=CH, O=TWINT, CN=Twint Mobile CA";
        private const string SigningCertSubjectName = "C=CH, O=TWINT, CN=Signing Identity";

        /// <summary>
        /// Create a new self-signed CA and signing certificate and store them in the given files.
        /// </summary>
        /// <param name="caCert">name of the CA certificate in PEM format</param>
        /// <param name="signingCert">name of the signing certificate in PEM format</param>
        /// <param name="signingCertWithPrivateKey">name of the signing certificate in PFX format</param>
        public static void CreateCertificates(string caCert, string signingCert, string signingCertWithPrivateKey)
        {
            using (RSA parent = RSA.Create(2048))
            using (RSA rsa = RSA.Create(2048))
            {
                CertificateRequest parentReq = new CertificateRequest(
                    CaCertSubjectName,
                    parent,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                parentReq.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                using (X509Certificate2 parentCert = parentReq.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddYears(10)))
                {
                    // export the CA certificate as PEM (public key only)
                    File.WriteAllText(caCert, parentCert.ExportCertificatePem());

                    CertificateRequest req = new CertificateRequest(
                        SigningCertSubjectName,
                        rsa,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);

                    using (X509Certificate2 cert = req.Create(
                        parentCert,
                        DateTimeOffset.UtcNow.AddDays(-1),
                        DateTimeOffset.UtcNow.AddYears(2),
                        new byte[] { 0 }))
                    {
                        // export the singing certificate as PEM (public key only)
                        File.WriteAllText(signingCert, cert.ExportCertificatePem());

                        // export the signing certificate as PKCS12 including the private key
                        using (var cert2 = RSACertificateExtensions.CopyWithPrivateKey(cert, rsa))
                        {
                            File.WriteAllBytes(signingCertWithPrivateKey, cert2.Export(X509ContentType.Pkcs12));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Import a certificate from a file and return the SHA256 hash of its public key.
        /// </summary>
        /// <param name="certFile">certificate file</param>
        /// <returns>SHA256 hash of public key</returns>
        public static string GetPublicKeyFingerprint(string certFile)
        {
            if (File.Exists(certFile))
            {
                using (var hasher = SHA256.Create())
                using (var cert = new X509Certificate2(certFile))
                {
                    var hashBytes = hasher.ComputeHash(cert.GetPublicKey());
                    return hashBytes.Aggregate(String.Empty, (str, hashByte) => str + hashByte.ToString("x2"));
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Sign a string with the RSA private key of the given certificate.
        /// </summary>
        /// <param name="certFile">certificate file</param>
        /// <param name="data">data to sign</param>
        /// <returns>base64 encoded signature</returns>
        public static string Sign(string certFile, string data)
        {
            if (File.Exists(certFile))
            {
                using (var cert = new X509Certificate2(certFile))
                {
                    byte[] dataToSign = Encoding.ASCII.GetBytes(data);
                    byte[] signature = cert.GetRSAPrivateKey().SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    return Convert.ToBase64String(signature);
                }
            }

            return string.Empty;
        }
    }
}
