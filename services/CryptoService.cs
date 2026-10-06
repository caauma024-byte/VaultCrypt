using System.Security.Cryptography;

namespace VaultCrypt.Services
{
    public class CryptoService
    {
        public byte[] EncryptAES(byte[] data, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();

            aes.Key = key;
            aes.IV = iv;

            using MemoryStream memoryStream = new();

            using CryptoStream cryptoStream =
                new(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);

            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();

            return memoryStream.ToArray();
        }

        public byte[] DecryptAES(byte[] encryptedData, byte[] key, byte[] iv)
        {
            using Aes aes = Aes.Create();

            aes.Key = key;
            aes.IV = iv;

            using MemoryStream memoryStream = new();

            using CryptoStream cryptoStream =
                new(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);

            cryptoStream.Write(
                encryptedData,
                0,
                encryptedData.Length
            );

            cryptoStream.FlushFinalBlock();

            return memoryStream.ToArray();
        }

        public void GenerateRSAKeys(
            out string publicKey,
            out string privateKey)
        {
            using RSA rsa = RSA.Create(2048);

            publicKey = Convert.ToBase64String(
                rsa.ExportRSAPublicKey()
            );

            privateKey = Convert.ToBase64String(
                rsa.ExportRSAPrivateKey()
            );
        }
    }
}