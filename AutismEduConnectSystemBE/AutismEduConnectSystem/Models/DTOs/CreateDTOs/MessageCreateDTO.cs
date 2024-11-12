using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class MessageCreateDTO
    {
        public int ConversationId { get; set; }
        public string SenderId { get; set; }
        public ApplicationUser Sender { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
