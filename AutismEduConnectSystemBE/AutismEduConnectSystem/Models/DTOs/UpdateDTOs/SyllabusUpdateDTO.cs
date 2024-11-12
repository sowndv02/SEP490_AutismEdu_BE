using AutismEduConnectSystem.Models.DTOs.CreateDTOs;

namespace AutismEduConnectSystem.Models.DTOs.UpdateDTOs
{
    public class SyllabusUpdateDTO
    {
        public int Id { get; set; }
        public int AgeFrom { get; set; }
        public int AgeEnd { get; set; }
        public List<SyllabusExerciseCreateDTO> SyllabusExercises { get; set; }
    }
}
