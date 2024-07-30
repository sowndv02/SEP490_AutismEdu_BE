using System.Text;

namespace backend_api.Utils
{
    public class PasswordGenerator
    {
        private static Random random = new Random();

        public static string GeneratePassword(int length = 12, bool requireDigit = true, bool requireLowercase = true, bool requireUppercase = true, bool requireNonAlphanumeric = true)
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string nonAlphanumeric = "!@$?_-";

            StringBuilder password = new StringBuilder();
            StringBuilder characterSet = new StringBuilder();

            if (requireLowercase)
            {
                password.Append(lowercase[random.Next(lowercase.Length)]);
                characterSet.Append(lowercase);
            }

            if (requireUppercase)
            {
                password.Append(uppercase[random.Next(uppercase.Length)]);
                characterSet.Append(uppercase);
            }

            if (requireDigit)
            {
                password.Append(digits[random.Next(digits.Length)]);
                characterSet.Append(digits);
            }

            if (requireNonAlphanumeric)
            {
                password.Append(nonAlphanumeric[random.Next(nonAlphanumeric.Length)]);
                characterSet.Append(nonAlphanumeric);
            }

            int remainingLength = length - password.Length;
            for (int i = 0; i < remainingLength; i++)
            {
                password.Append(characterSet[random.Next(characterSet.Length)]);
            }

            return new string(password.ToString().OrderBy(c => random.Next()).ToArray());
        }
    }
}
