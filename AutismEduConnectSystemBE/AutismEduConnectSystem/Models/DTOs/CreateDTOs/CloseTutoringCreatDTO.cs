namespace AutismEduConnectSystem.Models.DTOs.CreateDTOs
{
    public class CloseTutoringCreatDTO
    {
        public int StudentProfileId { get; set; }
        public string FinalCondition { get; set; }
        public List<InitialAssessmentResultCreateDTO> FinalAssessmentResults { get; set; }
    }
}
