using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.Models.DTOs
{
    public class MessageDTO
    {
        public int Id { get; set; }
        public ConversationDetailDTO Conversation { get; set; }
        public ApplicationUserDTO Sender { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
