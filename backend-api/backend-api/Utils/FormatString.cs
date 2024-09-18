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
    }
}
