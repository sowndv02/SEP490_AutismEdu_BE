namespace AutismEduConnectSystem.DTOs
{
    public class ConversationDTO
    {
        public int Id { get; set; }
        public ApplicationUserDTO User { get; set; }
        public List<MessageDetailDTO> Messages { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
