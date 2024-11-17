namespace AutismEduConnectSystem.Models.DTOs
{
    public class ConversationDetailDTO
    {
        public int Id { get; set; }
        public TutorDTO Tutor { get; set; }
        public ApplicationUserDTO Parent { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
