using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static backend_api.SD;

namespace backend_api.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; }
        public string JwtTokenId { get; set; }
        public string Refresh_Token { get; set; }
        public bool IsValid { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string TokenType { get; set; } = SD.APPLICATION_REFRESH_TOKEN;
    }
}
