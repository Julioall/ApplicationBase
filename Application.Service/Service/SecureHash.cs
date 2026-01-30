using System.Security.Cryptography;

namespace Application.Service.Service.Security
{
    public static class SecureHash
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;

        public static string HashSecret(string secret)
        {
            ArgumentException.ThrowIfNullOrEmpty(secret);

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Derive(secret, salt);

            var payload = new byte[1 + SaltSize + HashSize];
            payload[0] = 1; // version marker
            Buffer.BlockCopy(salt, 0, payload, 1, SaltSize);
            Buffer.BlockCopy(hash, 0, payload, 1 + SaltSize, HashSize);
            return Convert.ToBase64String(payload);
        }

        public static bool Verify(string secret, string? encodedHash)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(encodedHash))
            {
                return false;
            }

            byte[] payload;
            try
            {
                payload = Convert.FromBase64String(encodedHash);
            }
            catch
            {
                return false;
            }

            if (payload.Length != 1 + SaltSize + HashSize)
            {
                return false;
            }

            var salt = new byte[SaltSize];
            var storedHash = new byte[HashSize];
            Buffer.BlockCopy(payload, 1, salt, 0, SaltSize);
            Buffer.BlockCopy(payload, 1 + SaltSize, storedHash, 0, HashSize);

            var computed = Derive(secret, salt);
            return CryptographicOperations.FixedTimeEquals(storedHash, computed);
        }

        private static byte[] Derive(string secret, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(secret, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(HashSize);
        }
    }
}
