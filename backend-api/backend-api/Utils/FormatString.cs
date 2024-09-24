namespace backend_api.Utils
{
    public class FormatString
    {
        public string FormatStringUpperCaseFirstChar(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return roleName; // Return as is if null or empty

            return char.ToUpper(roleName[0]) + roleName.Substring(1).ToLower();
        }

        public string FormatStringFormalName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            string[] words = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            return string.Join(" ", words);
        }
    }
}
