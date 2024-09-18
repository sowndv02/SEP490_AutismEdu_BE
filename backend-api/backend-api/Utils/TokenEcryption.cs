using System.Security.Cryptography;
using System.Text;

namespace backend_api.Utils
{
    public class TokenEcryption
    {

        private static string key = string.Empty; // Ensure this key is securely managed
        private readonly byte[] _key;
        private readonly byte[] _iv;
        public TokenEcryption(IConfiguration configuration)
        {
            key = configuration.GetValue<string>("APIConfig:KeyEncryption");
            if (key.Length != 16) // Adjust as needed for 24 or 32 bytes
            {
                throw new ArgumentException("Invalid key length. Key must be 16 bytes long.");
            }
            _key = Encoding.UTF8.GetBytes(key);

            // Use a unique IV for each encryption operation
            _iv = new byte[16]; // Example: use a static IV or retrieve it from a secure source
        }
        public string EncryptToken(string token)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] dateTimeBytes = Encoding.UTF8.GetBytes(token);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(dateTimeBytes, 0, dateTimeBytes.Length);
                    return Convert.ToBase64String(encryptedBytes); // Return encrypted string
                }
            }
        }

        public string DecryptToken(string encryptedDateTime)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    byte[] encryptedBytes = Convert.FromBase64String(encryptedDateTime);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                    string decryptedString = Encoding.UTF8.GetString(decryptedBytes);
                    return decryptedString; // Convert back to DateTime
                }
            }
        }
    }
}
