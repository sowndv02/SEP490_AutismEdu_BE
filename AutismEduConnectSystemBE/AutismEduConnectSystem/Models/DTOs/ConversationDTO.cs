namespace AutismEduConnectSystem.Models.DTOs
{
    public class ConversationDTO
    {
        public int Id { get; set; }
        public TutorDTO Tutor { get; set; }
        public ApplicationUserDTO Parent { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
