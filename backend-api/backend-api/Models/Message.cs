using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend_api.Models
{
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ConversationId{ get; set; }
        [ForeignKey(nameof(ConversationId))]
        public Conversation Conversation { get; set; }
        public string SenderId { get; set; }
        [ForeignKey(nameof(SenderId))]
        public ApplicationUser Sender { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
