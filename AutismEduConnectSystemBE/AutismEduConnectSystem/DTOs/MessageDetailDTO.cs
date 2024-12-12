namespace AutismEduConnectSystem.DTOs
{
    public class MessageDetailDTO
    {
        public int Id { get; set; }
        public ApplicationUserDTO Sender { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
