using System.Text.Json.Serialization;

namespace AutismEduConnectSystem.Models
{
    public class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("verified_email")]
        public bool VerifiedEmail { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("picture")]
        public string Picture { get; set; }

        [JsonPropertyName("locale")]
        public string Locale { get; set; }
    }

}
