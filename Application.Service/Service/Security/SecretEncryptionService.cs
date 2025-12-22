using System.Security.Cryptography;
using System.Text;
using Application.Domain;
using Application.Domain.Exceptions;
using Application.Domain.Model;
using Application.Service.Interface;
using Microsoft.Extensions.Localization;

namespace Application.Service.Service.Security
{
    /// <summary>
    /// AES encryption helper backed by an environment-provided key so secrets can be stored reversibly.
    /// </summary>
    public class SecretEncryptionService : ISecretEncryptionService
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly byte[] _key;

        public SecretEncryptionService(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
            var keyMaterial = Environment.GetEnvironmentVariable(ApplicationConstants.SECRET_ENCRYPTION_KEY);
            if (string.IsNullOrWhiteSpace(keyMaterial))
            {
                throw new ConfigurationException(_localizer["EncryptionKeyNotConfigured", ApplicationConstants.SECRET_ENCRYPTION_KEY]);
            }

            _key = DeriveKey(keyMaterial);
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            using var aes = CreateAes();
            aes.GenerateIV();
            using var encryptor = aes.CreateEncryptor(_key, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // prepend IV to payload
            var payload = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, payload, aes.IV.Length, cipherBytes.Length);
            return Convert.ToBase64String(payload);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrWhiteSpace(cipherText))
            {
                return string.Empty;
            }

            byte[] payload;
            try
            {
                payload = Convert.FromBase64String(cipherText);
            }
            catch (FormatException)
            {
                throw new BusinessException(_localizer["EncryptedDataInvalidBase64"]);
            }

            using var aes = CreateAes();
            var ivLength = aes.BlockSize / 8;
            if (payload.Length <= ivLength)
            {
                throw new BusinessException(_localizer["EncryptedDataTooShort"]);
            }

            var iv = new byte[ivLength];
            var cipherBytes = new byte[payload.Length - ivLength];
            Buffer.BlockCopy(payload, 0, iv, 0, ivLength);
            Buffer.BlockCopy(payload, ivLength, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor(_key, iv);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private static Aes CreateAes()
        {
            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return aes;
        }

        private static byte[] DeriveKey(string keyMaterial)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(keyMaterial));
        }
    }
}
