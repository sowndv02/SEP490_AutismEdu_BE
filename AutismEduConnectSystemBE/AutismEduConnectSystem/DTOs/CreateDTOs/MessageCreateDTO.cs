using System.ComponentModel.DataAnnotations.Schema;

namespace AutismEduConnectSystem.DTOs.CreateDTOs
{
    public class MessageCreateDTO
    {
        public int ConversationId { get; set; }
        public string Content { get; set; }
    }
}
